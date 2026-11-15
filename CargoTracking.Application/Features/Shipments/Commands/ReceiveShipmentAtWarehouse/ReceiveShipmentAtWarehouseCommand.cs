using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Commands.ReceiveShipmentAtWarehouse;

//kargo göndericiden alındıktan sonra depoya ulaştığında çalışıyor.
public sealed record ReceiveShipmentAtWarehouseCommand(
       Guid ShipmentId) : IRequest;
public sealed class ReceiveShipmentAtWarehouseCommandHandler : IRequestHandler<ReceiveShipmentAtWarehouseCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public ReceiveShipmentAtWarehouseCommandHandler(IApplicationDbContext context, ISender sender, ICurrentUserService currentUserService)
    {
        _context = context;
        _sender = sender;
        _currentUserService = currentUserService;
    }

    public async Task Handle(ReceiveShipmentAtWarehouseCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (userId is null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "User must be authenticated.");
        }

        var warehouseEmployee = await _context.Users
     .FirstOrDefaultAsync(
         x => x.Id == userId.Value,
         cancellationToken);

        if (warehouseEmployee is null ||
            !warehouseEmployee.IsActive ||
            warehouseEmployee.Role != UserRole.WarehouseEmployee)
        {
            throw new UnauthorizedAccessException(
                "Only active warehouse employees can receive shipments.");
        }

        // Personelin bağlı olduğu depo profilini bul.
        var employeeProfile = await _context.WareHouseEmployeeProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (employeeProfile is null)
        {
            throw new UnauthorizedAccessException(
                "Warehouse employee profile does not exist.");
        }

        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException(
                "Shipment does not exist.");
        }
        //if (shipment.PickupCourierId != courier.Id)
        //{
        //    throw new UnauthorizedAccessException(
        //        "This shipment is not assigned to you.");
        //}
        if (shipment.Status != ShipmentStatus.PickedUpFromSender&& shipment.Status != ShipmentStatus.InTransit&& shipment.Status != ShipmentStatus.DeliveryFailed)
        {
            throw new InvalidOperationException(
               "Shipment cannot be received at warehouse.");
        }

        if (shipment.Status == ShipmentStatus.PickedUpFromSender)
        {
            // İlk depo kabulü
            var nearestWarehouse = await _sender.Send(new SelectNearestWarehouseQuery(ShipmentId: shipment.Id, IsInternalCall: true), cancellationToken);

            // Personelin deposu ile kargonun yönlendirildiği depoyu karşılaştır.
            if (employeeProfile.WareHouseId != nearestWarehouse.WarehouseId)
            {
                throw new UnauthorizedAccessException(
                    "You can only receive shipments at your assigned warehouse.");
            }

            if (shipment.PickupCourierId is null)
            {
                throw new InvalidOperationException(
                    "Shipment does not have an assigned pickup courier.");
            }
            var pickupCourier = await _context.CourierProfiles
    .FirstOrDefaultAsync(
        x => x.Id == shipment.PickupCourierId.Value,
        cancellationToken);

            if (pickupCourier is null)
            {
                throw new InvalidOperationException(
                    "Pickup courier profile does not exist.");
            }

            // Kurye kargoyu depoya teslim etti, artık müsait.
            pickupCourier.SetAvailability(true);

            // ChangeStatus, PickedUpFromSender -> AtWarehouse geçişini gerçekleştirir.
            shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

            // Kargoya mevcut depo atanır.
            shipment.AssignWarehouse(nearestWarehouse.WarehouseId);
        }
        else if (shipment.Status == ShipmentStatus.InTransit)
        {
            // Depolar arası transfer kabulü
            if (shipment.TargetWarehouseId is null)
            {
                throw new InvalidOperationException(
                    "Shipment does not have a target warehouse.");
            }

            if (employeeProfile.WareHouseId != shipment.TargetWarehouseId)
            {
                throw new UnauthorizedAccessException(
                    "You can only receive shipments at your assigned target warehouse.");
            }

            shipment.CompleteWarehouseTransfer();
            shipment.ChangeStatus(ShipmentStatus.AtWarehouse);
        }
        else if (shipment.Status == ShipmentStatus.DeliveryFailed)
        {
            // Başarısız teslimat sonrası depoya iade kabulü.
            if (shipment.CurrentWarehouseId is null)
            {
                throw new InvalidOperationException(
                    "Shipment does not have a current warehouse.");
            }

            // Personelin bağlı olduğu depo, kargonun mevcut deposu olmalı.
            if (employeeProfile.WareHouseId != shipment.CurrentWarehouseId)
            {
                throw new UnauthorizedAccessException(
                    "You can only receive returned shipments at their current warehouse.");
            }
            if (shipment.DeliveryCourierId is null)
            {
                throw new InvalidOperationException(
                    "Shipment does not have an assigned delivery courier.");
            }

            // Kargoyu depoya geri getiren önceki teslimat kuryesini bul.
            var deliveryCourier = await _context.CourierProfiles
                .FirstOrDefaultAsync(
                    x => x.Id == shipment.DeliveryCourierId.Value,
                    cancellationToken);

            if (deliveryCourier is null)
            {
                throw new InvalidOperationException(
                    "Delivery courier profile does not exist.");
            }
            // Kargo depoya ulaştığı için kurye artık müsait.
            deliveryCourier.SetAvailability(true);

            // DeliveryFailed -> AtWarehouse
            shipment.ChangeStatus(ShipmentStatus.AtWarehouse);
        }

        // Kargo durum geçmişine depo kabul kaydı eklenir.
        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment received at warehouse.",
            shipment.CurrentWarehouseId);

        await _context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        // Tüm değişiklikleri tek seferde kaydet.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
