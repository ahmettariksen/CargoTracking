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
public sealed class AutoAssignReturnCourierCommandHandlerTests
{
    //Başarılı kurye atama.
    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignReturnCourier()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var handler = new AutoAssignReturnCourierCommandHandler(
            context,
            new FakeSender(new List<Guid> { scenario.CourierProfile.Id }),
            new FakeCurrentUserService
            {
                UserId = scenario.WarehouseUser.Id
            });

        var command = new AutoAssignReturnCourierCommand(
            scenario.Shipment.Id);

        // Act
        var result = await handler.Handle(
            command,
            CancellationToken.None);

        // Assert
        Assert.Equal(scenario.CourierProfile.Id, result);
        Assert.Equal(
            scenario.CourierProfile.Id,
            scenario.Shipment.ReturnCourierId);

        Assert.False(scenario.CourierProfile.IsAvailable);

        Assert.Single(await context.OutboxMessages.ToListAsync());
        Assert.Single(await context.ShipmentTrackingEvents.ToListAsync());
    }
    //Giriş yapmamış kullanıcının atama yapamaması.
    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var handler = new AutoAssignReturnCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()),
            new FakeCurrentUserService());

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new AutoAssignReturnCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "User must be authenticated.",
            exception.Message);
    }
    //Gönderi bulunamadığında hata verilmesi.
    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var handler = new AutoAssignReturnCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()),
            new FakeCurrentUserService
            {
                UserId = scenario.WarehouseUser.Id
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignReturnCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal("Shipment does not exist.", exception.Message);
    }
    //Başka depoya ait gönderiye kurye atanamaması.
    [Fact]
    public async Task Handle_ShipmentBelongsToAnotherWarehouse_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(
            context,
            wrongWarehouse: true);

        var handler = new AutoAssignReturnCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()),
            new FakeCurrentUserService
            {
                UserId = scenario.WarehouseUser.Id
            });

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new AutoAssignReturnCourierCommand(scenario.Shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "You can only assign return couriers for shipments in your assigned warehouse.",
            exception.Message);
    }
    //Yakında uygun kurye bulunamaması.
    [Fact]
    public async Task Handle_NoNearbyCouriers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var scenario = await CreateScenario(context);

        var handler = new AutoAssignReturnCourierCommandHandler(
            context,
            new FakeSender(Array.Empty<Guid>()),
            new FakeCurrentUserService
            {
                UserId = scenario.WarehouseUser.Id
            });

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignReturnCourierCommand(scenario.Shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "No available couriers found near the warehouse.",
            exception.Message);

        Assert.Null(scenario.Shipment.ReturnCourierId);
        Assert.True(scenario.CourierProfile.IsAvailable);
        Assert.Empty(await context.OutboxMessages.ToListAsync());
        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    private static async Task<TestScenario> CreateScenario(
        IApplicationDbContext context,
        bool wrongWarehouse = false)
    {
        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "warehouse@test.com",
            "HASHED_password",
            "05551111111",
            UserRole.WarehouseEmployee);

        var warehouse = new Warehouse(
            "Düzce Depo",
            "Düzce",
            "Merkez",
            "Sanayi Caddesi",
            40.8438,
            31.1565);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id,
            wrongWarehouse ? Guid.NewGuid() : warehouse.Id);

        var senderUser = new User(
            "Veli",
            "Kaya",
            "sender@test.com",
            "HASHED_password",
            "05554444444",
            UserRole.Customer);

        var receiverUser = new User(
            "Ali",
            "Kaya",
            "receiver@test.com",
            "HASHED_password",
            "05553333333",
            UserRole.Customer);

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

        var senderAddress = new Address(
            senderUser.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Düzce Merkez",
            40.8438,
            31.1565);

        var receiverAddress = new Address(
            receiverUser.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Cedidiye",
            "Düzce Merkez",
            40.8438,
            31.1565);

        var shipment = new Shipment(
            "CT123456",
            senderUser.Id,
            receiverUser.Id,
            senderAddress.Id,
            receiverAddress.Id,
            5,
            DateTime.Now.AddDays(2));

        shipment.AssignWarehouse(warehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        await context.Users.AddRangeAsync(
            warehouseUser,
            senderUser,
            receiverUser,
            courierUser);

        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.CourierProfiles.AddAsync(courierProfile);

        await context.Addresses.AddRangeAsync(
            senderAddress,
            receiverAddress);

        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        return new TestScenario(
            warehouseUser,
            shipment,
            courierProfile);
    }

    private sealed record TestScenario(
        User WarehouseUser,
        Shipment Shipment,
        CourierProfile CourierProfile);
}
