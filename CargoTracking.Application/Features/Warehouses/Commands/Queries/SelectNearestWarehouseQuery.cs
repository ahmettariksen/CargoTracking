using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Warehouses.Commands.Queries;

//En yakın aktif depoyu bulur, depo bilgilerini döndürür
public sealed record SelectNearestWarehouseQuery(
    Guid ShipmentId,
    bool IsInternalCall=false) : IRequest<NearestWarehouseResponse>;
public sealed record NearestWarehouseResponse(
    Guid WarehouseId,
    string Name,
    string City,
    string District,
    string Address,
    double Latitude,
    double Longitude);
public sealed class SelectNearestWarehouseQueryHandler(
    IApplicationDbContext _context,
    ICurrentUserService currentUserService) : IRequestHandler<SelectNearestWarehouseQuery, NearestWarehouseResponse>
{
    public async Task<NearestWarehouseResponse> Handle(SelectNearestWarehouseQuery request, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException(
                "Shipment does not exist.");
        }

        if (!request.IsInternalCall)
        {
            var userId = currentUserService.UserId;

            if (userId is null || userId == Guid.Empty)
            {
                throw new UnauthorizedAccessException(
                    "Kimliği doğrulanmış kullanıcı bulunamadı.");
            }
            // Kullanıcı yalnızca kendi gönderisinin depo bilgilerini görebilir.
            if (shipment.SenderId != userId.Value)
            {
                throw new UnauthorizedAccessException(
                    "Bu kargo için depo bilgilerine erişim yetkiniz yok.");
            }
        }

        var senderAddress = await _context.Addresses
           .AsNoTracking()
           .FirstOrDefaultAsync(
               x => x.Id == shipment.SenderAddressId,
               cancellationToken);

        if (senderAddress is null)
        {
            throw new InvalidOperationException(
                "Sender address does not exist.");
        }

        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        if (warehouses.Count == 0)
        {
            throw new InvalidOperationException(
                "No active warehouse is available.");
        }
        var nearestWarehouse = warehouses
            .MinBy(warehouse =>
                CalculateDistance(
                    senderAddress.Latitude,
                    senderAddress.Longitude,
                    warehouse.Latitude,
                    warehouse.Longitude));

        if (nearestWarehouse is null)
        {
            throw new InvalidOperationException(
                "En yakın depo belirlenemedi.");
        }

        return new NearestWarehouseResponse(
            nearestWarehouse.Id,
            nearestWarehouse.Name,
            nearestWarehouse.City,
            nearestWarehouse.District,
            nearestWarehouse.Address,
            nearestWarehouse.Latitude,
            nearestWarehouse.Longitude);
    }
    private static double CalculateDistance(double latitude1, double longitude1, double latitude2, double longitude2)
    {
        const double earthRadiusKm = 6371;

        var lat1 = latitude1 * Math.PI / 180;
        var lat2 = latitude2 * Math.PI / 180;

        var deltaLat =
            (latitude2 - latitude1) * Math.PI / 180;

        var deltaLon =
            (longitude2 - longitude1) * Math.PI / 180;

        var a =
            Math.Pow(Math.Sin(deltaLat / 2), 2) +
            Math.Cos(lat1) *
            Math.Cos(lat2) *
            Math.Pow(Math.Sin(deltaLon / 2), 2);

        var c = 2 * Math.Atan2(
            Math.Sqrt(a),
            Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }
}
