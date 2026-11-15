using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Application.Features.Shipments.Events;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Shipments.CreateShipments;
public sealed record CreateShipmentCommand(
    //Guid SenderId, => currentuservice yapılanmasından sonra pasif ettik, kaldırdık SenderId istemciden gelirse kullanıcı başka birinin kimliğini göndermeyi deneyebilir. Bunun yerine gönderenin kimliğini JWT üzerinden alacağız.
    Guid ReceiverId,
    Guid SenderAddressId,
    Guid ReceiverAddressId,
    decimal Weight,
    DateTime? EstimatedDeliveryDate) : IRequest<Guid>;
//record: İstek verisini taşımak için uygun, kısa bir veri tipi oluşturur.
//sealed: Bu sınıftan kalıtım alınmasını engeller.
//IRequest<Guid>: MediatR'a bu isteğin çalıştırıldığında Guid tipinde bir sonuç döndüreceğini söyler. Handler bu sonucu yeni kargonun kimliği olarak döndürecek.
//SenderId ve ReceiverId: Gönderici ve alıcının kullanıcı kimlikleri.
//SenderAddressId ve ReceiverAddressId: Kullanılacak adres kayıtlarının kimlikleri.
//Weight: Kargonun kilogram cinsinden ağırlığı.
//EstimatedDeliveryDate: Tahmini teslim tarihi.

public sealed class CreateShipmentCommandHandler : IRequestHandler<CreateShipmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISender _sender;

    public CreateShipmentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _sender = sender;
    }

    public async Task<Guid> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        //Gönderici ile alıcının aktif kullanıcılar olduğunu doğruladık.
        //Adreslerin gerçekten ilgili kullanıcılara ait olduğunu kontrol ettik.
        //Sunucu tarafında takip numarası ürettik.
        //Kargo ve ilk takip olayını kaydettik.

        // Göndericinin kimliğini JWT üzerinden al.
        var senderId = _currentUserService.UserId;

        if (senderId is null)
        {
            throw new UnauthorizedAccessException(
                "Kimliği doğrulanmış kullanıcı bulunamadı.");
        }

        if (senderId.Value == request.ReceiverId)
        {
            throw new InvalidOperationException(
                "Gönderen ve alıcı aynı kullanıcı olamaz.");
        }

        // Gönderici ve alıcının aktif olduğunu doğrula.
        var senderExists = await _context.Users.AnyAsync(
            x => x.Id == senderId.Value && x.IsActive,
            cancellationToken);

        if (!senderExists)
        {
            throw new InvalidOperationException(
                "Sender does not exist or is inactive.");
        }

        var receiverExists = await _context.Users.AnyAsync(
            x => x.Id == request.ReceiverId && x.IsActive,
            cancellationToken);

        if (!receiverExists)
        {
            throw new InvalidOperationException(
                "Receiver does not exist or is inactive.");
        }

        // Adreslerin doğru kullanıcılara ait olduğunu doğrula.
        var senderAddress = await _context.Addresses
            .FirstOrDefaultAsync(
                x => x.Id == request.SenderAddressId &&
                     x.UserId == senderId.Value,
                cancellationToken);

        if (senderAddress is null)
        {
            throw new InvalidOperationException(
                "Sender address does not belong to the sender.");
        }

        var receiverAddressExists = await _context.Addresses
            .AnyAsync(
                x => x.Id == request.ReceiverAddressId &&
                     x.UserId == request.ReceiverId,
                cancellationToken);

        if (!receiverAddressExists)
        {
            throw new InvalidOperationException(
                "Receiver address does not belong to the receiver.");
        }

        // Kargoyu oluştur.
        var trackingNumber = $"CT{Guid.NewGuid():N}".ToUpperInvariant();

        var shipment = new Shipment(
            trackingNumber,
            senderId.Value,
            request.ReceiverId,
            request.SenderAddressId,
            request.ReceiverAddressId,
            request.Weight,
            request.EstimatedDeliveryDate);

        // Otomatik kurye ataması için kargoyu kabul et.

        var createdEvent = new ShipmentTrackingEvent(
            shipment.Id,
            ShipmentStatus.Created,
            "Shipment created.");

        var shipmentCreatedEvent = new ShipmentCreatedEvent(
            shipment.Id,
            shipment.TrackingNumber,
            shipment.SenderId,
            shipment.ReceiverId,
            shipment.Status.ToString(),
            shipment.CreatedAt);


        var payload = JsonSerializer.Serialize(shipmentCreatedEvent);

        var outboxMessage = new OutboxMessage(
            nameof(ShipmentCreatedEvent),
            payload);

        await _context.Shipments.AddAsync(
            shipment, cancellationToken);

        await _context.ShipmentTrackingEvents.AddAsync(createdEvent, cancellationToken);

        await _context.OutboxMessages.AddAsync(
            outboxMessage, cancellationToken);

        // Kargoyu ve başlangıç olaylarını önce kaydet.
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            // En yakın 10 aday arasından uygun alım kuryesini ata.
            await _sender.Send(
                new AutoAssignPickupCourierCommand(
                    shipment.Id,
                    RadiusKm: 500,
                    Count: 10),
                cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            var errorMessage = ex.Message;
            shipment.ChangeStatus(ShipmentStatus.Cancelled);

            var cancellationEvent = new ShipmentTrackingEvent(
                shipment.Id,
                ShipmentStatus.Cancelled,
                "Shipment cancelled because no available pickup courier could be assigned.");

            await _context.ShipmentTrackingEvents.AddAsync(
                cancellationEvent, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        return shipment.Id;
    }
}
