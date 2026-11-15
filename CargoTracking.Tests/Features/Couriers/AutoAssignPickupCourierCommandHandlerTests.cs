using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class AutoAssignPickupCourierCommandHandlerTests
{
    // Yakın kurye bulunamazsa hata vermeli.
    [Fact]
    public async Task Handle_NoNearbyCouriers_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var senderUser = CreateCustomerUser("sender");
        var receiverUser = CreateCustomerUser("receiver");

        await context.Users.AddRangeAsync(senderUser, receiverUser);

        var senderAddress = CreateAddress(senderUser.Id);
        var receiverAddress = CreateAddress(receiverUser.Id);

        await context.Addresses.AddRangeAsync(
            senderAddress, receiverAddress);

        var shipment = CreateShipment(
            senderUser.Id,
            receiverUser.Id,
            senderAddress.Id,
            receiverAddress.Id);

        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new AutoAssignPickupCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()));

        var command = new AutoAssignPickupCourierCommand(
            shipment.Id, 5, 10);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(
            "No available nearby couriers were found.",
            exception.Message);

        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Null(shipment.PickupCourierId);
        Assert.Null(shipment.DeliveryCourierId);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    // Kargo bulunamazsa hata vermeli.
    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var handler = new AutoAssignPickupCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()));

        var command = new AutoAssignPickupCourierCommand(
            Guid.NewGuid(), 5, 10);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("Shipment does not exist.", exception.Message);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    // Kurye zaten atanmışsa tekrar atanmamalı.
    [Fact]
    public async Task Handle_PickupCourierAlreadyAssigned_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var senderUser = CreateCustomerUser("sender");
        var receiverUser = CreateCustomerUser("receiver");

        await context.Users.AddRangeAsync(senderUser, receiverUser);

        var senderAddress = CreateAddress(senderUser.Id);
        var receiverAddress = CreateAddress(receiverUser.Id);

        await context.Addresses.AddRangeAsync(
            senderAddress, receiverAddress);

        var shipment = CreateShipment(
            senderUser.Id,
            receiverUser.Id,
            senderAddress.Id,
            receiverAddress.Id);

        var existingCourierId = Guid.NewGuid();
        shipment.AssignPickupCourier(existingCourierId);

        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new AutoAssignPickupCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()));

        var command = new AutoAssignPickupCourierCommand(
            shipment.Id, 5, 10);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(
            "A pickup courier has already been assigned.",
            exception.Message);

        Assert.Equal(existingCourierId, shipment.PickupCourierId);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }
    // Yakındaki kurye başarıyla bulunuyor.
    //Kurye gönderiye atanıyor.
    //Gönderinin durumu Accepted oluyor.
    //Kurye müsaitliği kapatılıyor.
    //E-posta bildirimi Outbox'a ekleniyor
    //Takip geçmişine atama olayı ekleniyor.
    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignPickupCourier()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var senderUser = CreateCustomerUser("sender");
        var receiverUser = CreateCustomerUser("receiver");

        var courierUser = new User(
            "Mehmet",
            "Demir",
            "courier@test.com",
            "HASHED_password",
            "05552222222",
            UserRole.Courier);

        var senderAddress = CreateAddress(senderUser.Id);
        var receiverAddress = CreateAddress(receiverUser.Id);

        var shipment = CreateShipment(
            senderUser.Id,
            receiverUser.Id,
            senderAddress.Id,
            receiverAddress.Id);

        var courierProfile = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 123");

        var nearestWarehouse = new NearestWarehouseResponse(
            Guid.NewGuid(),
            "Düzce Depo",
            "Düzce",
            "Merkez",
            "Sanayi Caddesi",
            40.8438,
            31.1565);

        await context.Users.AddRangeAsync(
            senderUser,
            receiverUser,
            courierUser);

        await context.Addresses.AddRangeAsync(
            senderAddress,
            receiverAddress);

        await context.Shipments.AddAsync(shipment);
        await context.CourierProfiles.AddAsync(courierProfile);

        await context.SaveChangesAsync();

        var fakeSender = new FakePickupCourierSender(
            new List<Guid> { courierProfile.Id },
            nearestWarehouse);

        var handler = new AutoAssignPickupCourierCommandHandler(
            context,
            fakeSender);

        var command = new AutoAssignPickupCourierCommand(
            shipment.Id,
            5,
            10);

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(courierProfile.Id, result);
        Assert.Equal(courierProfile.Id, shipment.PickupCourierId);
        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);
        Assert.False(courierProfile.IsAvailable);

        Assert.Single(await context.OutboxMessages.ToListAsync());
        Assert.Single(await context.ShipmentTrackingEvents.ToListAsync());
    }

    private static User CreateCustomerUser(string prefix)
    {
        return new User(
            "Test",
            prefix,
            $"{prefix}-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            "05550000000",
            UserRole.Customer);
    }

    private static Address CreateAddress(Guid userId)
    {
        return new Address(
            userId,
            "Home",
            "Düzce",
            "Merkez",
            "Test Mahallesi",
            "Test adresi",
            40.8438,
            31.1565);
    }

    private static Shipment CreateShipment(
        Guid senderId,
        Guid receiverId,
        Guid senderAddressId,
        Guid receiverAddressId)
    {
        return new Shipment(
            $"CT{Guid.NewGuid():N}".ToUpperInvariant(),
            senderId,
            receiverId,
            senderAddressId,
            receiverAddressId,
            2.5m,
            DateTime.Now.AddDays(3));
    }
}
