using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Commands.TransferShipment;
public sealed record TransferShipmentCommand(
    Guid ShipmentId,
    Guid TargetWarehouseId) : IRequest<Guid>;

//Kullanıcının oturumunu doğrulama.
//Kullanıcının aktif depo personeli olduğunu doğrulama.
//Personelin bağlı olduğu kaynak depoyu bulma.
//Kargonun mevcut durumunun AtWarehouse olduğunu kontrol etme.
//Kargonun CurrentWarehouseId değerinin personelin deposuyla eşleşmesini kontrol etme.
//Hedef deponun varlığını ve aktifliğini kontrol etme.
//Hedef deponun mevcut depodan farklı olduğunu kontrol etme.
//TargetWarehouseId değerini atama.
//Kargo durumunu InTransit yapma.
//Tracking event oluşturup kaydetme.
public sealed class TransferShipmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<TransferShipmentCommand, Guid>
{
    public async Task<Guid> Handle(TransferShipmentCommand request, CancellationToken cancellationToken)
    {
        // 1. Kullanıcı doğrulaması
        var userId = currentUserService.UserId;

        if (userId is null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "User must be authenticated.");
        }

        // 2. Depo personelinin kontrolü
        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId.Value,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.Role != UserRole.WarehouseEmployee)
        {
            throw new UnauthorizedAccessException(
                "Only active warehouse employees can transfer shipments.");
        }

        // 3. Personelin bağlı olduğu depo
        var employeeProfile = await context.WareHouseEmployeeProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (employeeProfile is null)
        {
            throw new UnauthorizedAccessException(
                "Warehouse employee profile does not exist.");
        }

        // 4. Kargoyu bul
        var shipment = await context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException(
                "Shipment does not exist.");
        }

        // 5. Kargo depoda olmalı
        if (shipment.Status != ShipmentStatus.AtWarehouse)
        {
            throw new InvalidOperationException(
                "Only shipments at a warehouse can be transferred.");
        }

        // 6. Kargonun mevcut deposu personelin deposu olmalı
        if (shipment.CurrentWarehouseId != employeeProfile.WareHouseId)
        {
            throw new UnauthorizedAccessException(
                "You can only transfer shipments from your assigned warehouse.");
        }
        if (shipment.TargetWarehouseId is not null)
        {
            throw new InvalidOperationException(
                "Shipment already has a target warehouse.");
        }

        // 7. Hedef depoyu bul
        var targetWarehouse = await context.Warehouses
            .FirstOrDefaultAsync(
                x => x.Id == request.TargetWarehouseId,
                cancellationToken);

        if (targetWarehouse is null)
        {
            throw new InvalidOperationException(
                "Target warehouse does not exist.");
        }

        if (!targetWarehouse.IsActive)
        {
            throw new InvalidOperationException(
                "Target warehouse is not active.");
        }

        // 8. Aynı depoya transfer yapılamaz
        if (shipment.CurrentWarehouseId == request.TargetWarehouseId)
        {
            throw new InvalidOperationException(
                "Target warehouse cannot be the current warehouse.");
        }

        // 9. Hedef depoyu ata ve transferi başlat
        shipment.AssingTargetWarehouse(request.TargetWarehouseId);
        shipment.ChangeStatus(ShipmentStatus.InTransit);

        // 10. Takip geçmişine kayıt ekle
        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            $"Shipment transferred from warehouse {shipment.CurrentWarehouseId} to warehouse {request.TargetWarehouseId}.",
            shipment.CurrentWarehouseId);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
