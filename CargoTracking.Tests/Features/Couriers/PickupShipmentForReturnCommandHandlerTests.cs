using CargoTracking.Application.Abstractions.Persistence;
using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
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
public sealed class PickupShipmentForReturnCommandHandlerTests
{
    //Başarılı iade kargosu teslim alma.
    [Fact]
    public async Task Handle_ValidCommand_ShouldPickupReturnShipment()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var handler = new PickupShipmentForReturnCommandHandler(
            context,
            new FakeCurrentUserService
            {
                UserId = scenario.CourierUser.Id
            });

        // Act
        var result = await handler.Handle(
            new PickupShipmentForReturnCommand(scenario.Shipment.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(scenario.Shipment.Id, result);
        Assert.Equal(ShipmentStatus.PickedUp, scenario.Shipment.Status);
        Assert.Single(await context.ShipmentTrackingEvents.ToListAsync());
        Assert.False(scenario.CourierProfile.IsAvailable);
    }
    //Kimliği doğrulanmamış kullanıcı.
    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var handler = new PickupShipmentForReturnCommandHandler(
            context,
            new FakeCurrentUserService());

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new PickupShipmentForReturnCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal("User must be authenticated.", exception.Message);
    }
    //Gönderiye atanmış iade kuryesinin işlemi yapmaması (yetkisiz kurye).
    [Fact]
    public async Task Handle_ShipmentAssignedToAnotherCourier_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var anotherCourierUser = new User(
            "Ali",
            "Demir",
            "anothercourier@test.com",
            "HASHED_password",
            "05553333333",
            UserRole.Courier);

        var anotherCourierProfile = new CourierProfile(
            anotherCourierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 456");

        await context.Users.AddAsync(anotherCourierUser);
        await context.CourierProfiles.AddAsync(anotherCourierProfile);
        await context.SaveChangesAsync();

        var handler = new PickupShipmentForReturnCommandHandler(
            context,
            new FakeCurrentUserService
            {
                UserId = anotherCourierUser.Id
            });

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new PickupShipmentForReturnCommand(scenario.Shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "This shipment is not assigned to you for return.",
            exception.Message);

        Assert.Equal(
            ShipmentStatus.AssignedToCourier,
            scenario.Shipment.Status);

        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }
    //Gönderi bulunamaması.
    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var handler = new PickupShipmentForReturnCommandHandler(
            context,
            new FakeCurrentUserService
            {
                UserId = scenario.CourierUser.Id
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new PickupShipmentForReturnCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal("Shipment does not exist.", exception.Message);
    }

        private static async Task<TestScenario> CreateScenario(
            IApplicationDbContext context)
        {
            var courierUser = new User(
                "Mehmet",
                "Demir",
                "courier@test.com",
                "HASHED_password",
                "05552222222",
                UserRole.Courier);

            var courierProfile = new CourierProfile(
                courierUser.Id,
                VehicleType.Motorcycle,
                "81 ABC 123");

            var sender = new User(
                "Veli",
                "Kaya",
                "sender@test.com",
                "HASHED_password",
                "05554444444",
                UserRole.Customer);

            var receiver = new User(
                "Ali",
                "Kaya",
                "receiver@test.com",
                "HASHED_password",
                "05553333333",
                UserRole.Customer);

            var senderAddress = new Address(
                sender.Id,
                "Ev",
                "Düzce",
                "Merkez",
                "Aziziye",
                "Düzce Merkez",
                40.8438,
                31.1565);

            var receiverAddress = new Address(
                receiver.Id,
                "Ev",
                "Düzce",
                "Merkez",
                "Cedidiye",
                "Düzce Merkez",
                40.8438,
                31.1565);
            var warehouse = new Warehouse(
        "Düzce Depo",
        "Düzce",
        "Merkez",
        "Sanayi Caddesi",
        40.8438,
        31.1565);

            var shipment = new Shipment(
                "CT123456",
                sender.Id,
                receiver.Id,
                senderAddress.Id,
                receiverAddress.Id,
                5,
                DateTime.Now.AddDays(2));

            shipment.AssignWarehouse(warehouse.Id);
            shipment.ChangeStatus(ShipmentStatus.Accepted);
            shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

            shipment.AssignReturnCourier(courierProfile.Id);

            await context.Users.AddRangeAsync(
                courierUser,
                sender,
                receiver);

            await context.CourierProfiles.AddAsync(courierProfile);

            await context.Addresses.AddRangeAsync(
                senderAddress,
                receiverAddress);

            await context.Shipments.AddAsync(shipment);
            await context.Warehouses.AddAsync(warehouse);

            await context.SaveChangesAsync();

            return new TestScenario(
                courierUser,
                courierProfile,
                shipment);
        }

    private sealed record TestScenario(
        User CourierUser,
        CourierProfile CourierProfile,
        Shipment Shipment);
}
