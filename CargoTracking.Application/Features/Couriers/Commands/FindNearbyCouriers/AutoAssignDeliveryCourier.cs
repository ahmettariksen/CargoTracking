using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Events;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
public sealed record AutoAssignDeliveryCourierCommand(
    Guid ShipmentId) : IRequest<Guid>;

public sealed class AutoAssignDeliveryCourierCommandHandler(
    IApplicationDbContext context,
    ISender sender,
    ICurrentUserService currentUserService)
    : IRequestHandler<AutoAssignDeliveryCourierCommand, Guid>
{
    public async Task<Guid> Handle(
        AutoAssignDeliveryCourierCommand request,
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
            user.Role != UserRole.WarehouseEmployee)
        {
            throw new UnauthorizedAccessException(
                "Only active warehouse employees can assign delivery couriers.");
        }

        var employeeProfile = await context.WareHouseEmployeeProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (employeeProfile is null)
            throw new UnauthorizedAccessException(
                "Warehouse employee profile does not exist.");

        var shipment = await context.Shipments
            .Include(x => x.ReceiverAddress)
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
            throw new InvalidOperationException(
                "Shipment does not exist.");

        if (shipment.CurrentWarehouseId != employeeProfile.WareHouseId)
            throw new UnauthorizedAccessException(
                "You can only assign delivery couriers for shipments in your assigned warehouse.");

        if (shipment.DeliveryCourierId is not null)
            throw new InvalidOperationException(
                "A delivery courier has already been assigned.");

        if (shipment.Status != ShipmentStatus.AtWarehouse)
            throw new InvalidOperationException(
                "Only shipments at a warehouse can be assigned to a delivery courier.");

        var warehouse = await context.Warehouses
            .FirstOrDefaultAsync(
                x => x.Id == employeeProfile.WareHouseId,
                cancellationToken);

        if (warehouse is null || !warehouse.IsActive)
            throw new InvalidOperationException(
                "Assigned warehouse does not exist or is inactive.");

        const double radiusKm = 250;
        const int count = 10;

        var nearbyCourierIds = await sender.Send(
            new FindNearbyCouriersQuery(
                warehouse.Latitude,
                warehouse.Longitude,
                radiusKm,
                count),
            cancellationToken);

        if (nearbyCourierIds.Count == 0)
            throw new InvalidOperationException(
                "No available couriers found near the warehouse.");

        foreach (var courierId in nearbyCourierIds)
        {
            var courier = await context.CourierProfiles
                .FirstOrDefaultAsync(
                    x => x.Id == courierId && x.IsAvailable,
                    cancellationToken);

            if (courier is null)
                continue;

            var courierUser = await context.Users
                .FirstOrDefaultAsync(
                    x => x.Id == courier.UserId &&
                         x.IsActive &&
                         x.Role == UserRole.Courier,
                    cancellationToken);

            if (courierUser is null)
                continue;

            shipment.AssignDeliveryCourier(courier.Id);
            courier.SetAvailability(false);

            var emailEvent = new CourierAssignmentEmailEvent(
    courierUser.Email,
    shipment.TrackingNumber,
    shipment.ReceiverAddress.FullAddress,
    warehouse.Name,
    warehouse.Address);

            var payload = JsonSerializer.Serialize(emailEvent);

            var outboxMessage = new OutboxMessage(
                nameof(CourierAssignmentEmailEvent),
                payload);

            await context.OutboxMessages.AddAsync(
                outboxMessage,
                cancellationToken);

            var trackingEvent = new ShipmentTrackingEvent(
                shipment.Id,
                shipment.Status,
                "Delivery courier assigned.",
                shipment.CurrentWarehouseId,
                courier.Id);

            await context.ShipmentTrackingEvents.AddAsync(
                trackingEvent,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            return courier.Id;
        }

        throw new InvalidOperationException(
            "No eligible delivery courier found.");
    }
}