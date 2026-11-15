using CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
using CargoTracking.Application.Features.Shipments.Commands.AssignCourier;
using CargoTracking.Application.Features.Shipments.CreateShipments;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Shipments.Commands.CreateShipment;
public sealed class MarkPickedUpFromSenderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenAssignedCourierPicksUpShipment_UpdatesStatusAndAddsTrackingEvent()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var courierUser = new User(
            "Test",
            "Courier",
            $"courier-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            "05550000000",
            UserRole.Courier);

        var courier = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 123");

        var shipment = CreateShipment();

        shipment.AssignPickupCourier(courier.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        await context.Users.AddAsync(courierUser);
        await context.CourierProfiles.AddAsync(courier);
        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new MarkPickedUpFromSenderCommandHandler(context);

        // Act
        await handler.Handle(
            new MarkPickedUpFromSenderCommand(shipment.Id, courier.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.PickedUpFromSender, shipment.Status);

        var trackingEvent = await context.ShipmentTrackingEvents
            .SingleAsync(x => x.ShipmentId == shipment.Id);

        Assert.Equal(ShipmentStatus.PickedUpFromSender, trackingEvent.Status);
        Assert.Equal(courier.Id, trackingEvent.CourierId);
        Assert.Equal(
            "Shipment picked up from sender.",
            trackingEvent.Description);
    }

    [Fact]
    public async Task Handle_WhenDifferentCourierAttemptsPickup_ThrowsException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var assignedUser = CreateCourierUser();
        var assignedCourier = CreateCourierProfile(assignedUser.Id);

        var otherUser = CreateCourierUser();
        var otherCourier = CreateCourierProfile(otherUser.Id);

        var shipment = CreateShipment();
        shipment.AssignPickupCourier(assignedCourier.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        await context.Users.AddRangeAsync(assignedUser, otherUser);
        await context.CourierProfiles.AddRangeAsync(assignedCourier, otherCourier);
        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new MarkPickedUpFromSenderCommandHandler(context);

        // Act + Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new MarkPickedUpFromSenderCommand(shipment.Id, otherCourier.Id),
                CancellationToken.None));

        Assert.Equal(
            "Only the assigned pickup courier can pick up this shipment.",
            exception.Message);

        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);

        Assert.Empty(await context.ShipmentTrackingEvents
            .Where(x => x.ShipmentId == shipment.Id)
            .ToListAsync());
    }

    [Fact]
    public async Task Handle_WhenShipmentIsNotAccepted_ThrowsException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var courierUser = CreateCourierUser();
        var courier = CreateCourierProfile(courierUser.Id);

        var shipment = CreateShipment();
        shipment.AssignPickupCourier(courier.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        // Kargo artık Accepted durumunda değil.
        // Test için farklı bir duruma geçmek üzere mevcut durumdan
        // geçerli bir domain geçişi kullanılmalıdır.
        // Bu testin aşağıdaki hâli AtWarehouse durumunu kullanır.
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        await context.Users.AddAsync(courierUser);
        await context.CourierProfiles.AddAsync(courier);
        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new MarkPickedUpFromSenderCommandHandler(context);

        // Act + Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new MarkPickedUpFromSenderCommand(shipment.Id, courier.Id),
                CancellationToken.None));

        Assert.Equal(
            "Shipment must be accepted before pickup.",
            exception.Message);

        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
    }

    private static Shipment CreateShipment()
    {
        return new Shipment(
            $"CT{Guid.NewGuid():N}".ToUpperInvariant(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2.5m,
            DateTime.Now.AddDays(3));
    }

    private static User CreateCourierUser() =>
        new(
            "Test",
            "Courier",
            $"courier-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            "05550000000",
            UserRole.Courier);

    private static CourierProfile CreateCourierProfile(Guid userId) =>
        new(userId, VehicleType.Motorcycle, "81 ABC 123");
}
