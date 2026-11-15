using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Commands.AssignCourier;
public sealed record AssignCourierCommand(
     Guid ShipmentId,
     Guid CourierId) : IRequest;
public sealed class AssignCourierCommandHandler : IRequestHandler<AssignCourierCommand>
{
    private readonly IApplicationDbContext _context;

    public AssignCourierCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task Handle(AssignCourierCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
            throw new InvalidOperationException(
                "Shipment does not exist.");

        if (shipment.Status != ShipmentStatus.AtWarehouse)
            throw new InvalidOperationException(
                "Shipment must be at a warehouse before courier assignment.");

        var courier = await _context.CourierProfiles
            .FirstOrDefaultAsync(
                x => x.Id == request.CourierId,
                cancellationToken);

        if (courier is null)
            throw new InvalidOperationException(
                "Courier profile does not exist.");

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.Id == courier.UserId,
                cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.Role != UserRole.Courier)
        {
            throw new InvalidOperationException(
                "Courier user is invalid, inactive, or does not have the Courier role.");
        }

        if (!courier.IsAvailable)
            throw new InvalidOperationException(
                "Courier is not available.");

        // Bu command depodan sonraki teslimat kuryesini atar.
        shipment.AssignDeliveryCourier(courier.Id);

        //shipment.AssignCourier(courier.Id);
        courier.SetAvailability(false);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment assigned to courier.",
            shipment.CurrentWarehouseId,
            courier.Id);

        await _context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
