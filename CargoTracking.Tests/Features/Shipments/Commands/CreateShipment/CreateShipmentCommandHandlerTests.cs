using CargoTracking.Application.Features.Shipments.CreateShipments;
using CargoTracking.Application.Features.Shipments.Events;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Shipments.Commands.CreateShipment;
public sealed class CreateShipmentCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateShipmentAndTrackingEvent()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Gönderici adresi", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy",
            "Caferağa", "Alıcı adresi", 40.9900, 29.0300);

        await context.Users.AddRangeAsync(sender, receiver);
        await context.Addresses.AddRangeAsync(senderAddress, receiverAddress);
        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            receiver.Id,
            senderAddress.Id,
            receiverAddress.Id,
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService
        {
            UserId = sender.Id
        };

        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());

        var handler = new CreateShipmentCommandHandler(
            context,
            currentUserService,
            fakeSender);

        // Act
        var shipmentId = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert: Kargo
        var shipment = await context.Shipments
            .SingleOrDefaultAsync(x => x.Id == shipmentId);

        Assert.NotNull(shipment);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Equal(sender.Id, shipment.SenderId);
        Assert.Equal(receiver.Id, shipment.ReceiverId);
        Assert.Equal(2.5m, shipment.Weight);
        Assert.False(string.IsNullOrWhiteSpace(shipment.TrackingNumber));

        // Assert: Başlangıç takip olayları
        var trackingEvents = await context.ShipmentTrackingEvents
            .Where(x => x.ShipmentId == shipmentId)
            .ToListAsync();

        Assert.Single(trackingEvents);

        Assert.Contains(trackingEvents, x =>
            x.Status == ShipmentStatus.Created &&
            x.Description == "Shipment created.");

        // Assert: Outbox mesajı
        var outboxMessage = await context.OutboxMessages
            .SingleOrDefaultAsync(
                x => x.Type == nameof(ShipmentCreatedEvent));

        Assert.NotNull(outboxMessage);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(0, outboxMessage.RetryCount);
        Assert.False(string.IsNullOrWhiteSpace(outboxMessage.Payload));

        var payloadEvent = JsonSerializer.Deserialize<ShipmentCreatedEvent>(
            outboxMessage.Payload);

        Assert.NotNull(payloadEvent);
        Assert.Equal(shipmentId, payloadEvent.ShipmentId);
        Assert.Equal(shipment.TrackingNumber, payloadEvent.TrackingNumber);
        Assert.Equal(sender.Id, payloadEvent.SenderId);
        Assert.Equal(receiver.Id, payloadEvent.ReceiverId);
        Assert.Equal(ShipmentStatus.Created.ToString(), payloadEvent.Status);
    }
    // 2. Test: Gönderici bulunamadığında hata vermeli
    [Fact]
    public async Task Handle_SenderDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy",
            "Caferağa", "Alıcı adresi", 40.9900, 29.0300);

        await context.Users.AddAsync(receiver);
        await context.Addresses.AddAsync(receiverAddress);
        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            //Guid.NewGuid(), // Veritabanında bulunmayan gönderici
            receiver.Id,
            Guid.NewGuid(), // Gönderici adresi de bulunmuyor
            receiverAddress.Id,
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService { UserId = Guid.NewGuid() };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sender does not exist or is inactive.", exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    // 3.Test Alıcı bulunamadığında kargo oluşturulmamalı.
    [Fact]
    public async Task Handle_ReceiverDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Gönderici adresi", 40.8438, 31.1565);

        await context.Users.AddAsync(sender);
        await context.Addresses.AddAsync(senderAddress);
        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            // sender.Id,
            Guid.NewGuid(), // Veritabanında bulunmayan alıcı
            senderAddress.Id,
            Guid.NewGuid(),
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService { UserId = sender.Id };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Receiver does not exist or is inactive.", exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());

    }

    //4. Test: Gönderici adresi başka bir kullanıcıya aitse hata vermeli
    [Fact]
    public async Task Handle_SenderAddressBelongsToAnotherUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var anotherUser = new User(
            "Ali", "Kaya", "ali@example.com",
            "hashed-password", "05553333333", UserRole.Customer);

        var anotherUsersAddress = new Address(
            anotherUser.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Başka kullanıcıya ait adres", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy",
            "Caferağa", "Alıcı adresi", 40.9900, 29.0300);

        await context.Users.AddRangeAsync(sender, receiver, anotherUser);
        await context.Addresses.AddRangeAsync(anotherUsersAddress, receiverAddress);
        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            //sender.Id,
            receiver.Id,
            anotherUsersAddress.Id, // Göndericiye ait olmayan adres
            receiverAddress.Id,
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService { UserId = sender.Id };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Sender address does not belong to the sender.",
            exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());

    }

    //Alıcı adresi başka bir kullanıcıya aitse işlem reddedilmeli.
    [Fact]
    public async Task Handle_ReceiverAddressBelongsToAnotherUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var anotherUser = new User(
            "Ali", "Kaya", "ali@example.com",
            "hashed-password", "05553333333", UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Gönderici adresi", 40.8438, 31.1565);

        var anotherUsersAddress = new Address(
            anotherUser.Id, "Ev", "Düzce", "Merkez",
            "Aziziye", "Başka kullanıcıya ait adres", 40.8438, 31.1565);

        await context.Users.AddRangeAsync(sender, receiver, anotherUser);
        await context.Addresses.AddRangeAsync(senderAddress, anotherUsersAddress);
        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            //sender.Id,
            receiver.Id,
            senderAddress.Id,
            anotherUsersAddress.Id, // Alıcıya ait olmayan adres
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService { UserId = sender.Id };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Receiver address does not belong to the receiver.",
            exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());

    }

    //6. Test: Pasif göndericiyle kargo oluşturulmamalı
    [Fact]
    public async Task Handle_InactiveSender_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Gönderici adresi", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy",
            "Caferağa", "Alıcı adresi", 40.9900, 29.0300);

        await context.Users.AddRangeAsync(sender, receiver);
        await context.Addresses.AddRangeAsync(senderAddress, receiverAddress);

        // Test verisinde göndericiyi pasif hale getiriyoruz.
        context.Entry(sender)
            .Property(x => x.IsActive)
            .CurrentValue = false;

        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            //sender.Id,
            receiver.Id,
            senderAddress.Id,
            receiverAddress.Id,
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService { UserId = sender.Id };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Sender does not exist or is inactive.", exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());

    }

    // 7. Test: Pasif alıcıyla kargo oluşturulmamalı
    [Fact]
    public async Task Handle_InactiveReceiver_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var sender = new User(
            "Ahmet", "Yılmaz", "ahmet@example.com",
            "hashed-password", "05551111111", UserRole.Customer);

        var receiver = new User(
            "Mehmet", "Demir", "mehmet@example.com",
            "hashed-password", "05552222222", UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez",
            "Cumhuriyet", "Gönderici adresi", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy",
            "Caferağa", "Alıcı adresi", 40.9900, 29.0300);

        await context.Users.AddRangeAsync(sender, receiver);
        await context.Addresses.AddRangeAsync(senderAddress, receiverAddress);

        context.Entry(receiver)
            .Property(x => x.IsActive)
            .CurrentValue = false;

        await context.SaveChangesAsync();

        var command = new CreateShipmentCommand(
            //sender.Id,
            receiver.Id,
            senderAddress.Id,
            receiverAddress.Id,
            2.5m,
            DateTime.Now.AddDays(3));

        var currentUserService = new FakeCurrentUserService
        {
            UserId = sender.Id,
        };
        var fakeSender = new CreateShipmentFakeSender(Guid.NewGuid());
        var handler = new CreateShipmentCommandHandler(context, currentUserService, fakeSender);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Receiver does not exist or is inactive.", exception.Message);

        Assert.Empty(await context.Shipments.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());

    }

    //Bu sahte servis, testte JWT ile giriş yapmış kullanıcıyı temsil edecek.
    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId { get; set; }
    }
}

