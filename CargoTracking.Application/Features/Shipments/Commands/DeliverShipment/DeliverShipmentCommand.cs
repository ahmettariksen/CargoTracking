using CargoTracking.Application.Abstractions.Persistence;
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

namespace CargoTracking.Application.Features.Shipments.Commands.DeliverShipment;
public sealed record DeliverShipmentCommand(
    Guid ShipmentId) : IRequest<Guid>;

//Yalnızca kargoya atanmış teslimat kuryesi işlem yapabilecek.
//Kargo PickedUp durumunda olmalı.
//Kurye kargoyu teslim ettiğinde Delivered durumuna geçecek.
//DeliveredAt otomatik olarak güncellenecek.
//Tracking event oluşturulacak.
//Kurye tekrar müsait (IsAvailable = true) yapılacak.
public sealed class DeliverShipmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeliverShipmentCommand, Guid>
{
    public async Task<Guid> Handle(
        DeliverShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (userId is null || userId == Guid.Empty)
            throw new UnauthorizedAccessException(
                "User must be authenticated.");

        var user = await context.Users
            .FirstOrDefaultAsync(
                x => x.Id == userId.Value,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.Role != UserRole.Courier)
        {
            throw new UnauthorizedAccessException(
                "Only active couriers can deliver shipments.");
        }

        var courier = await context.CourierProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (courier is null)
            throw new UnauthorizedAccessException(
                "Courier profile does not exist.");

        var shipment = await context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
            throw new InvalidOperationException(
                "Shipment does not exist.");

        if (shipment.DeliveryCourierId != courier.Id)
            throw new UnauthorizedAccessException(
                "This shipment is not assigned to you.");

        if (shipment.Status != ShipmentStatus.PickedUp)
            throw new InvalidOperationException(
                "Shipment must be out for delivery before it can be delivered.");

        shipment.ChangeStatus(ShipmentStatus.Delivered);

        courier.SetAvailability(true);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment successfully delivered to receiver.",
            shipment.CurrentWarehouseId,
            courier.Id);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
