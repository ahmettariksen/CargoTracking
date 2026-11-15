using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.Commands.PickupShipment;
public sealed record PickupShipmentCommand(
    Guid ShipmentId) : IRequest<Guid>;
// kargonun kurye tarafından alındığını kontrol eder ve veritabına yansıtır.
//1. Oturum açmış kullanıcının ID'sini alıyor.
//2. Kullanıcının kurye profili olup olmadığını kontrol ediyor.
//3. Kargoyu veritabanından getiriyor.
//4. Kargonun gerçekten bu kuryeye atanmış olduğunu doğruluyor.
//5. MarkPickedUpFromSender() metodunu çağırıyor.
//6. Takip geçmişine kayıt ekliyor.
//7. Değişiklikleri kaydediyor.
//CurrentWarehouseId bu aşamada NULL kalmaya devam edecek.
public sealed class PickupShipmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<PickupShipmentCommand, Guid>
{
    public async Task<Guid> Handle(
        PickupShipmentCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        if (userId is null || userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException(
                "User must be authenticated.");
        }

        var courier = await context.CourierProfiles
            .FirstOrDefaultAsync(
                x => x.UserId == userId.Value,
                cancellationToken);

        if (courier is null)
        {
            throw new UnauthorizedAccessException(
                "Only couriers can pick up shipments.");
        }

        var shipment = await context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException(
                "Shipment does not exist.");
        }

        if (shipment.PickupCourierId != courier.Id)
        {
            throw new UnauthorizedAccessException(
                "This shipment is not assigned to you.");
        }

        shipment.MarkPickedUpFromSender();

        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment picked up from sender.",
            shipment.CurrentWarehouseId,
            courier.Id);

        await context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return shipment.Id;
    }
}
