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

namespace CargoTracking.Application.Features.Shipments.Commands.PickupShipment;

// teslimat kuryesinin kargoyu depodan teslim alması işlemlerini kontrol eder
public sealed record PickupShipmentForDeliveryCommand(
    Guid ShipmentId) : IRequest<Guid>;

//Kullanıcı giriş yapmış olmalı.
//Kullanıcı aktif bir kurye olmalı.
//Kurye profili bulunmalı.
//Kargo AssignedToCourier durumunda olmalı.
//Kargonun DeliveryCourierId değeri giriş yapan kuryenin profil ID'siyle eşleşmeli.
//Kargo durumu PickedUp yapılmalı.
//Tracking event oluşturulmalı.
//Değişiklikler kaydedilmeli.
public sealed class PickupShipmentForDeliveryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<PickupShipmentForDeliveryCommand, Guid>
{
    public async Task<Guid> Handle(
        PickupShipmentForDeliveryCommand request,
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
                "Only active couriers can pick up shipments for delivery.");
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

        if (shipment.Status != ShipmentStatus.AssignedToCourier)
            throw new InvalidOperationException(
                "Shipment is not waiting for courier pickup.");

        if (shipment.DeliveryCourierId != courier.Id)
            throw new UnauthorizedAccessException(
                "This shipment is not assigned to you.");

        shipment.ChangeStatus(ShipmentStatus.PickedUp);

        courier.SetAvailability(false);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment picked up from warehouse by delivery courier.",
            shipment.CurrentWarehouseId,
            courier.Id);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}

