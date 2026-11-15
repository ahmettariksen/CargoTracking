using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Events;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
public sealed record AutoAssignPickupCourierCommand(
    // Otomatik alım kuryesi atama işlemi için gerekli parametreleri taşır.
    // ShipmentId: Kurye atanacak kargonun kimliği.
    // RadiusKm: Kargonun gönderici adresi çevresinde aranacak mesafe (km).
    // Count: Yakın kurye sorgusundan en fazla kaç kurye alınacağı.
    Guid ShipmentId,
    double RadiusKm,
    int Count = 10) : IRequest<Guid>; // İşlem sonucunda atanan kuryenin ID'si döndürülür.

// Komutun iş kurallarını uygulayan handler sınıfıdır.
// IApplicationDbContext: Veritabanı işlemlerine erişim sağlar.
// ISender: Mevcut FindNearbyCouriersQuery sorgusunu MediatR üzerinden çalıştırır.
public sealed class AutoAssignPickupCourierCommandHandler(
    IApplicationDbContext context,
    ISender sender)
    /*ICourierLocationStore courierLocationStore)*/ : IRequestHandler<AutoAssignPickupCourierCommand, Guid>
{
    public async Task<Guid> Handle(AutoAssignPickupCourierCommand request, CancellationToken cancellationToken)
    {

        if (request.RadiusKm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.RadiusKm),
                "Radius must be greater than zero.");
        }

        // Aday sayısı pozitif olmalıdır.
        if (request.Count <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.Count),
                "Count must be greater than zero.");
        }

        // Kargoyu ve gönderici adresini getir.
        var shipment = await context.Shipments
            .Include(x => x.SenderAddress)
            .FirstOrDefaultAsync(
                x => x.Id == request.ShipmentId,
                cancellationToken);

        if (shipment is null)
        {
            throw new InvalidOperationException(
                "Shipment does not exist.");
        }
        // Daha önce kurye atanmışsa tekrar atama yapma.
        if (shipment.PickupCourierId.HasValue)
        {
            throw new InvalidOperationException(
                "A pickup courier has already been assigned.");
        }

        // Yalnızca oluşturulan kargolara alım kuryesi atanabilir.
        if (shipment.Status != ShipmentStatus.Created)
        {
            throw new InvalidOperationException(
                "Only newly created shipments can receive a pickup courier.");
        }
        // Redis üzerinden gönderici adresine yakın kuryeleri bul.
        var nearbyCourierIds = await sender.Send(
            new FindNearbyCouriersQuery(
                shipment.SenderAddress.Latitude,
                shipment.SenderAddress.Longitude,
                request.RadiusKm,
                request.Count),
            cancellationToken);

        // Hiç aday bulunamadıysa başarısızlık bildir.
        if (nearbyCourierIds.Count == 0)
        {
            throw new InvalidOperationException(
                "No available nearby couriers were found.");
        }

        // Yakınlık sorgusundan gelen adayları sırayla kontrol et.
        foreach (var courierId in nearbyCourierIds)
        {
            // Kurye profilini veritabanından getir.
            var courier = await context.CourierProfiles
                .FirstOrDefaultAsync(
                    c => c.Id == courierId,
                    cancellationToken);

            // Profil bulunmuyorsa veya kurye müsait değilse
            // sıradaki adayı dene.
            if (courier is null || !courier.IsAvailable)
            {
                continue;
            }

            // Kurye kullanıcısını getir.
            var user = await context.Users
                .FirstOrDefaultAsync(
                    u => u.Id == courier.UserId,
                    cancellationToken);

            // Kullanıcı aktif ve Courier rolünde olmalıdır.
            if (user is null ||
                !user.IsActive ||
                user.Role != UserRole.Courier)
            {
                continue;
            }

            // İlk uygun kuryeyi kargoya ata.
            shipment.AssignPickupCourier(courier.Id);
            shipment.ChangeStatus(ShipmentStatus.Accepted);

            // En yakın aktif depoyu belirle.
            var nearestWarehouse = await sender.Send(
                new SelectNearestWarehouseQuery(shipment.Id, IsInternalCall: true),
                cancellationToken);

            // Kurye bilgilendirme olayını oluştur.
            var emailEvent = new CourierAssignmentEmailEvent(
                user.Email,
                shipment.TrackingNumber,
                shipment.SenderAddress.FullAddress,
                nearestWarehouse.Name,
                nearestWarehouse.Address);

            // Olayı JSON formatına dönüştür.
            var payload = JsonSerializer.Serialize(emailEvent);

            // E-posta mesajını Outbox'a ekle.
            var outboxMessage = new OutboxMessage(
                nameof(CourierAssignmentEmailEvent),
                payload);

            await context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);

            // Atama sonrasında kuryeyi müsait değil olarak işaretle.
            courier.SetAvailability(false);

            // Atama olayını takip geçmişine ekle.
            var trackingEvent = new ShipmentTrackingEvent(
                shipment.Id,
                shipment.Status,
                "Pickup courier assigned.",
                shipment.CurrentWarehouseId,
                courier.Id);

            await context.ShipmentTrackingEvents.AddAsync(
                trackingEvent,
                cancellationToken);

            // Kargo, kurye müsaitliği ve takip olayını kaydet.
            await context.SaveChangesAsync(cancellationToken);

            // Atanan kuryenin ID'sini döndür.
            return courier.Id;
        }

        // Bütün adaylar kontrol edildi, uygun kurye bulunamadı.
        throw new InvalidOperationException(
            "No available nearby couriers could be assigned.");




        //    if (request.RadiusKm <= 0)
        //    {
        //        throw new ArgumentOutOfRangeException(
        //            nameof(request.RadiusKm),
        //            "Radius must be greater than zero.");
        //    }

        //    if (request.Count <= 0)
        //    {
        //        throw new ArgumentOutOfRangeException(
        //            nameof(request.Count),
        //            "Count must be greater than zero.");
        //    }

        //    var shipment = await context.Shipments
        //        .Include(x => x.SenderAddress)
        //        .FirstOrDefaultAsync(
        //            x => x.Id == request.ShipmentId,
        //            cancellationToken);

        //    if (shipment is null)
        //    {
        //        throw new InvalidOperationException(
        //            "Shipment does not exist.");
        //    }

        //    if (shipment.PickupCourierId.HasValue)
        //    {
        //        throw new InvalidOperationException(
        //            "A pickup courier has already been assigned.");
        //    }

        //    if (shipment.Status != ShipmentStatus.Created)
        //    {
        //        throw new InvalidOperationException(
        //            "Only newly created shipments can receive a pickup courier.");
        //    }

        //    // Aktif ve müsait kuryeleri SQL Server'dan getir.
        //    var availableCouriers = await (
        //        from courier in context.CourierProfiles
        //        join user in context.Users
        //            on courier.UserId equals user.Id
        //        where courier.IsAvailable
        //              && user.IsActive
        //              && user.Role == UserRole.Courier
        //              && courier.CurrentLatitude != null
        //              && courier.CurrentLongitude != null
        //        select courier
        //    ).ToListAsync(cancellationToken);

        //    // Mevcut kurye koordinatlarını Redis'e senkronize et.
        //    await courierLocationStore.SyncCourierLocationsAsync(
        //        availableCouriers);

        //    // Redis'ten gönderici adresine en yakın kuryeleri getir.
        //    var nearbyCourierIds = await sender.Send(
        //        new FindNearbyCouriersQuery(
        //            shipment.SenderAddress.Latitude,
        //            shipment.SenderAddress.Longitude,
        //            request.RadiusKm,
        //            request.Count),
        //        cancellationToken);

        //    if (nearbyCourierIds.Count == 0)
        //    {
        //        throw new InvalidOperationException(
        //            "No available nearby couriers were found.");
        //    }

        //    foreach (var courierId in nearbyCourierIds)
        //    {
        //        var courier = availableCouriers
        //            .FirstOrDefault(c => c.Id == courierId);

        //        if (courier is null || !courier.IsAvailable)
        //        {
        //            continue;
        //        }

        //        // Atamadan önce kullanıcının aktifliğini tekrar doğrula.
        //        var user = await context.Users
        //            .FirstOrDefaultAsync(
        //                u => u.Id == courier.UserId,
        //                cancellationToken);

        //        if (user is null ||
        //            !user.IsActive ||
        //            user.Role != UserRole.Courier)
        //        {
        //            continue;
        //        }

        //        shipment.AssignPickupCourier(courier.Id);
        //        shipment.ChangeStatus(ShipmentStatus.Accepted);
        //        courier.SetAvailability(false);

        //        var trackingEvent = new ShipmentTrackingEvent(
        //            shipment.Id,
        //            shipment.Status,
        //            "Pickup courier assigned.",
        //            shipment.CurrentWarehouseId,
        //            courier.Id);

        //        await context.ShipmentTrackingEvents.AddAsync(
        //            trackingEvent,
        //            cancellationToken);

        //        await context.SaveChangesAsync(cancellationToken);

        //        return courier.Id;
        //    }

        //    throw new InvalidOperationException(
        //        "No available nearby couriers could be assigned.");
        //}
        // Arama yarıçapı pozitif olmalıdır.
    }
}
