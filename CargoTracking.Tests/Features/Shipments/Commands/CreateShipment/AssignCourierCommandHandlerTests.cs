using CargoTracking.Application.Features.Shipments.Commands.AssignCourier;
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
public sealed class AssignCourierCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignCourier()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateShipmentAtWarehouse();
        var user = CreateCourierUser();
        var courier = CreateCourierProfile(user.Id);

        await context.Shipments.AddAsync(shipment);
        await context.Users.AddAsync(user);
        await context.CourierProfiles.AddAsync(courier);
        await context.SaveChangesAsync();

        var handler = new AssignCourierCommandHandler(context);
        var command = new AssignCourierCommand(
            shipment.Id,
            courier.Id);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(
            ShipmentStatus.AssignedToCourier,
            shipment.Status);

        Assert.Equal(courier.Id, shipment.DeliveryCourierId);
        Assert.False(courier.IsAvailable);

        var trackingEvent = await context.ShipmentTrackingEvents
            .SingleAsync(x => x.ShipmentId == shipment.Id);

        Assert.Equal(
            ShipmentStatus.AssignedToCourier,
            trackingEvent.Status);

        Assert.Equal(courier.Id, trackingEvent.CourierId);
        Assert.Equal(
            "Shipment assigned to courier.",
            trackingEvent.Description);
    }

    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowException()
    {
        await using var context = TestDbContextFactory.Create();

        var user = CreateCourierUser();
        var courier = CreateCourierProfile(user.Id);

        await context.Users.AddAsync(user);
        await context.CourierProfiles.AddAsync(courier);
        await context.SaveChangesAsync();

        var handler = new AssignCourierCommandHandler(context);
        var command = new AssignCourierCommand(
            Guid.NewGuid(),
            courier.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Shipment does not exist.", exception.Message);
        Assert.True(courier.IsAvailable);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    [Fact]
    public async Task Handle_CourierDoesNotExist_ShouldThrowException()
    {
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateShipmentAtWarehouse();
        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var handler = new AssignCourierCommandHandler(context);
        var command = new AssignCourierCommand(
            shipment.Id,
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Courier profile does not exist.", exception.Message);
        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    [Fact]
    public async Task Handle_CourierUnavailable_ShouldThrowException()
    {
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateShipmentAtWarehouse();
        var user = CreateCourierUser();
        var courier = CreateCourierProfile(user.Id);

        courier.SetAvailability(false);

        await context.Shipments.AddAsync(shipment);
        await context.Users.AddAsync(user);
        await context.CourierProfiles.AddAsync(courier);
        await context.SaveChangesAsync();

        var handler = new AssignCourierCommandHandler(context);
        var command = new AssignCourierCommand(
            shipment.Id,
            courier.Id);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Courier is not available.", exception.Message);
        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }
    //Kargo Accepted durumundayken teslimat kuryesi atanmaya çalışıldığında işlem reddedilmeli.
    [Fact]
    public async Task Handle_ShipmentNotAtWarehouse_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var shipment = new Shipment(
            $"CT{Guid.NewGuid():N}".ToUpperInvariant(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2.5m,
            DateTime.Now.AddDays(3));

        shipment.ChangeStatus(ShipmentStatus.Accepted);

        var user = CreateCourierUser();
        var courier = CreateCourierProfile(user.Id);

        await context.Shipments.AddAsync(shipment);
        await context.Users.AddAsync(user);
        await context.CourierProfiles.AddAsync(courier);
        await context.SaveChangesAsync();

        var handler = new AssignCourierCommandHandler(context);
        var command = new AssignCourierCommand(shipment.Id, courier.Id);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Shipment must be at a warehouse before courier assignment.",
            exception.Message);

        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);
        Assert.Null(shipment.DeliveryCourierId);
        Assert.True(courier.IsAvailable);
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    private static User CreateCourierUser()
    {
        return new User(
            "Test",
            "Courier",
            $"courier-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            "05550000000",
            UserRole.Courier);
    }

    private static CourierProfile CreateCourierProfile(Guid userId)
    {
        return new CourierProfile(
            userId,
            VehicleType.Motorcycle,
            "81 ABC 123");
    }

    private static Shipment CreateShipmentAtWarehouse()
    {
        var shipment = new Shipment(
            $"CT{Guid.NewGuid():N}".ToUpperInvariant(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            2.5m,
            DateTime.Now.AddDays(3));

        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        return shipment;
    }
}
