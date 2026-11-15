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

namespace CargoTracking.Application.Features.Shipments.Commands.UpdateShipmentStatus;

//ShipmentId: Hangi kargonun güncelleneceği.
//NewStatus: Kargonun yeni durumu.
//Description: Takip geçmişinde gösterilecek açıklama.
public sealed record UpdateShipmentStatusCommand(
     Guid ShipmentId,
     ShipmentStatus NewStatus,
     string Description) : IRequest<bool>;

internal sealed class UpdateShipmentStatusCommandHandler : IRequestHandler<UpdateShipmentStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateShipmentStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateShipmentStatusCommand request, CancellationToken cancellationToken)
    {
        var shipment = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == request.ShipmentId, cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException("Shipment was not found.");
        }
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            throw new InvalidOperationException(
                "Tracking event description cannot be empty.");
        }
        shipment.ChangeStatus(request.NewStatus);

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            request.Description.Trim());

        await _context.ShipmentTrackingEvents.AddAsync(trackingEvent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return true;

    }
}
