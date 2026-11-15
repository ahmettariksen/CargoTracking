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

//Yalnızca atanmış alım kuryesi kargoyu teslim alabiliyor.Durum PickedUpFromSender oluyor ve takip olayı kaydediliyor.
public sealed record MarkPickedUpFromSenderCommand(
    Guid ShipmentId,
    Guid CourierId) : IRequest;

public sealed class MarkPickedUpFromSenderCommandHandler
    : IRequestHandler<MarkPickedUpFromSenderCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkPickedUpFromSenderCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        MarkPickedUpFromSenderCommand request,
        CancellationToken cancellationToken)
    {
        // İşlem yapılacak kargoyu bul.
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
            throw new InvalidOperationException(
                "Shipment does not exist.");

        // İşlemi yapmaya çalışan kuryenin profilini bul.
        var courier = await _context.CourierProfiles
            .FirstOrDefaultAsync(
                x => x.Id == request.CourierId,
                cancellationToken);

        if (courier is null)
            throw new InvalidOperationException(
                "Courier profile does not exist.");

        // Kurye kullanıcısının aktif olduğunu ve Courier rolüne
        // sahip olduğunu doğrula.
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

        // Kargoyu yalnızca kendisine atanmış alım kuryesi alabilir.
        if (shipment.PickupCourierId != courier.Id)
            throw new InvalidOperationException(
                "Only the assigned pickup courier can pick up this shipment.");

        // Domain metodu, kargonun Accepted durumda olduğunu ve
        // alım kuryesinin atanmış olduğunu yeniden doğrular.
        shipment.MarkPickedUpFromSender();

        // Kargo takip geçmişine göndericiden teslim alma olayını ekle.
        var trackingEvent = new ShipmentTrackingEvent(
            shipment.Id,
            shipment.Status,
            "Shipment picked up from sender.",
            shipment.CurrentWarehouseId,
            courier.Id);

        await _context.ShipmentTrackingEvents.AddAsync(
            trackingEvent,
            cancellationToken);

        // Durum değişikliğini ve takip olayını kaydet.
        await _context.SaveChangesAsync(cancellationToken);
    }
}
