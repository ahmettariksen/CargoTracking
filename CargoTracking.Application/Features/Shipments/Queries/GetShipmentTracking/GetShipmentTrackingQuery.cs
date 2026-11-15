using CargoTracking.Application.Abstractions.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Queries.GetShipmentTracking;
public sealed record GetShipmentTrackingQuery(
    string TrackingNumber) : IRequest<ShipmentTrackingResponse?>;

public sealed record ShipmentTrackingResponse(
    string TrackingNumber,
    string CurrentStatus,
    DateTime CreatedAt,
    DateTime? EstimatedDeliveryDate,
    IReadOnlyList<ShipmentTrackingEventResponse> Events);

public sealed record ShipmentTrackingEventResponse(
    string Status,
    string Description,
    DateTime CreatedAt);

internal sealed class GetShipmentTrackingQueryHandler : IRequestHandler<GetShipmentTrackingQuery, ShipmentTrackingResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetShipmentTrackingQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<ShipmentTrackingResponse?> Handle(GetShipmentTrackingQuery request, CancellationToken cancellationToken)
    {
        var trackingNumber = request.TrackingNumber.Trim().ToUpperInvariant();

        var shipment = await _context.Shipments
            .AsNoTracking()
            .Where(x => x.TrackingNumber == trackingNumber)
            .Select(x => new
            {
                x.Id,
                x.TrackingNumber,
                x.Status,
                x.CreatedAt,
                x.EstimatedDeliveryDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (shipment is null)
        {
            return null;
        }

        var events = await _context.ShipmentTrackingEvents
            .AsNoTracking()
            .Where(x => x.ShipmentId == shipment.Id)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ShipmentTrackingEventResponse(
                x.Status.ToString(),
                x.Description,
                x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new ShipmentTrackingResponse(
            shipment.TrackingNumber,
            shipment.Status.ToString(),
            shipment.CreatedAt,
            shipment.EstimatedDeliveryDate,
            events);

    }
}

