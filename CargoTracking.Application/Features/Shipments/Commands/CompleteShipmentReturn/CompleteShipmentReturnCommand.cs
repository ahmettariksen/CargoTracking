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

namespace CargoTracking.Application.Features.Shipments.Commands.CompleteShipmentReturn;
public sealed record CompleteShipmentReturnCommand(
    Guid ShipmentId) : IRequest<Guid>;

public sealed class CompleteShipmentReturnCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CompleteShipmentReturnCommand, Guid>
{
    public async Task<Guid> Handle(
        CompleteShipmentReturnCommand request,
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
                "Only active couriers can complete shipment returns.");
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

        if (shipment.ReturnCourierId != courier.Id)
            throw new UnauthorizedAccessException(
                "This shipment is not assigned to you for return.");

        if (shipment.Status != ShipmentStatus.PickedUp)
            throw new InvalidOperationException(
                "Shipment must be picked up before completing return.");

        shipment.CompleteReturn();

        courier.SetAvailability(true);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment successfully returned to sender.",
            null,
            courier.Id);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
