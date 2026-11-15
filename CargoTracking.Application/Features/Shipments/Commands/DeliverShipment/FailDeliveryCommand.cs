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
public sealed record FailDeliveryCommand(
    Guid ShipmentId,
    string Reason) : IRequest<Guid>;

//Yalnızca aktif teslimat kuryesi işlem yapabilir.
//Kargo o kuryeye atanmış olmalı.
//Kargonun durumu PickedUp olmalı.
//Başarısızlık nedeni boş olamaz.
//Kargo DeliveryFailed durumuna geçmeli.
//Tracking event oluşturulmalı.
//Kurye müsaitlik durumu true yapılmalı.

public sealed class FailDeliveryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<FailDeliveryCommand, Guid>
{
    public async Task<Guid> Handle(
        FailDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException(
                "Delivery failure reason cannot be empty.");

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
                "Only active couriers can report failed deliveries.");
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
                "Only picked-up shipments can have a failed delivery.");

        shipment.ChangeStatus(ShipmentStatus.DeliveryFailed);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            $"Delivery failed: {request.Reason.Trim()}",
            shipment.CurrentWarehouseId,
            courier.Id);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
