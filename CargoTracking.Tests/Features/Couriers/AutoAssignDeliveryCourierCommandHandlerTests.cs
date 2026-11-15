using CargoTracking.Application.Features.Couriers.Commands.FindNearbyCouriers;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class AutoAssignDeliveryCourierCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldAssignDeliveryCourier()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password",
            "05551111111",
            UserRole.WarehouseEmployee);

        var courierUser = new User(
            "Mehmet",
            "Demir",
            "mehmet@test.com",
            "HASHED_password",
            "05552222222",
            UserRole.Courier);

        var warehouse = new Warehouse(
            "Düzce Depo",
            "Düzce",
            "Merkez",
            "Sanayi Caddesi",
            40.8438,
            31.1565);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id,
            warehouse.Id);

        var courierProfile = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 123");

        courierProfile.SetAvailability(true);

        var receiver = new User(
            "Ali",
            "Kaya",
            "ali@test.com",
            "HASHED_password",
            "05553333333",
            UserRole.Customer);

        var sender = new User(
            "Veli",
            "Kaya",
            "veli@test.com",
            "HASHED_password",
            "05554444444",
            UserRole.Customer);

        var receiverAddress = new Address(
            receiver.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Cedidiye",
            "Düzce Merkez",
            40.8438,
            31.1565);

        var senderAddress = new Address(
            sender.Id,
            "Ev",
            "Düzce",
            "Merkez",
            "Aziziye",
            "Düzce Merkez",
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

        await context.Users.AddRangeAsync(
            warehouseUser,
            courierUser,
            receiver,
            sender);

        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.CourierProfiles.AddAsync(courierProfile);
        await context.Addresses.AddRangeAsync(receiverAddress, senderAddress);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var fakeSender = new FakeSender(
            new List<Guid> { courierProfile.Id });

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act
        var result = await handler.Handle(
            new AutoAssignDeliveryCourierCommand(shipment.Id),
            CancellationToken.None);

        // Assert
        Assert.Equal(courierProfile.Id, result);
        Assert.Equal(courierProfile.Id, shipment.DeliveryCourierId);
        Assert.Equal(ShipmentStatus.AssignedToCourier, shipment.Status);
        Assert.False(courierProfile.IsAvailable);

        Assert.Single(context.OutboxMessages);
        Assert.Single(context.ShipmentTrackingEvents);
    }
    //Kimliği doğrulanmamış kullanıcı kurye ataması yapamamalı.
    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = null
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        var command = new AutoAssignDeliveryCourierCommand(Guid.NewGuid());

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal("User must be authenticated.", exception.Message);
    }
    //Aktif olmayan depo çalışanı kurye ataması yapamamalı.
    [Fact]
    public async Task Handle_InactiveWarehouseEmployee_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password",
            "05551111111",
            UserRole.WarehouseEmployee);

        warehouseUser.Deactivate();

        await context.Users.AddAsync(warehouseUser);
        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "Only active warehouse employees can assign delivery couriers.",
            exception.Message);
    }
    //Depo çalışanının profili yoksa kurye ataması yapılamamalı.
    [Fact]
    public async Task Handle_WarehouseEmployeeProfileDoesNotExist_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
            "HASHED_password",
            "05551111111",
            UserRole.WarehouseEmployee);

        await context.Users.AddAsync(warehouseUser);
        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "Warehouse employee profile does not exist.",
            exception.Message);
    }
    //Kargo bulunamazsa hata fırlatılmalı.
    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
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
            warehouse.Id);

        await context.Users.AddAsync(warehouseUser);
        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal("Shipment does not exist.", exception.Message);
    }
    //Kargo AtWarehouse durumunda değilse kurye ataması yapılamamalı.
    [Fact]
    public async Task Handle_ShipmentNotAtWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet",
            "Yılmaz",
            "ahmet@test.com",
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
            warehouse.Id);

        var sender = new User(
            "Veli",
            "Kaya",
            "veli@test.com",
            "HASHED_password",
            "05554444444",
            UserRole.Customer);

        var receiver = new User(
            "Ali",
            "Kaya",
            "ali@test.com",
            "HASHED_password",
            "05553333333",
            UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez", "Aziziye",
            "Düzce Merkez", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "Düzce", "Merkez", "Cedidiye",
            "Düzce Merkez", 40.8438, 31.1565);

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

        await context.Users.AddRangeAsync(
            warehouseUser, sender, receiver);

        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Addresses.AddRangeAsync(senderAddress, receiverAddress);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "Only shipments at a warehouse can be assigned to a delivery courier.",
            exception.Message);

        Assert.Null(shipment.DeliveryCourierId);
    }
    //Depo çalışanı başka bir depodaki kargoya kurye atayamamalı
    [Fact]
    public async Task Handle_ShipmentBelongsToAnotherWarehouse_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet", "Yılmaz", "ahmet@test.com",
            "HASHED_password", "05551111111",
            UserRole.WarehouseEmployee);

        var employeeWarehouse = new Warehouse(
            "Düzce Depo", "Düzce", "Merkez",
            "Sanayi Caddesi", 40.8438, 31.1565);

        var shipmentWarehouse = new Warehouse(
            "İstanbul Depo", "İstanbul", "Kadıköy",
            "Kadıköy Caddesi", 40.9900, 29.0200);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id,
            employeeWarehouse.Id);

        var sender = new User(
            "Veli", "Kaya", "veli@test.com",
            "HASHED_password", "05554444444",
            UserRole.Customer);

        var receiver = new User(
            "Ali", "Kaya", "ali@test.com",
            "HASHED_password", "05553333333",
            UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez", "Aziziye",
            "Düzce Merkez", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "İstanbul", "Kadıköy", "Moda",
            "İstanbul Kadıköy", 40.9900, 29.0200);

        var shipment = new Shipment(
            "CT123456", sender.Id, receiver.Id,
            senderAddress.Id, receiverAddress.Id,
            5, DateTime.Now.AddDays(2));

        shipment.AssignWarehouse(shipmentWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        await context.Users.AddRangeAsync(
            warehouseUser, sender, receiver);

        await context.Warehouses.AddRangeAsync(
            employeeWarehouse, shipmentWarehouse);

        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);

        await context.Addresses.AddRangeAsync(
            senderAddress, receiverAddress);

        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "You can only assign delivery couriers for shipments in your assigned warehouse.",
            exception.Message);

        Assert.Null(shipment.DeliveryCourierId);
    }
    //Kargoya daha önce kurye atanmışsa tekrar atama yapılamamalı.[Fact]
    [Fact]
    public async Task Handle_ShipmentAlreadyHasDeliveryCourier_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet", "Yılmaz", "ahmet@test.com",
            "HASHED_password", "05551111111",
            UserRole.WarehouseEmployee);

        var warehouse = new Warehouse(
            "Düzce Depo", "Düzce", "Merkez",
            "Sanayi Caddesi", 40.8438, 31.1565);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id, warehouse.Id);

        var sender = new User(
            "Veli", "Kaya", "veli@test.com",
            "HASHED_password", "05554444444",
            UserRole.Customer);

        var receiver = new User(
            "Ali", "Kaya", "ali@test.com",
            "HASHED_password", "05553333333",
            UserRole.Customer);

        var courierUser = new User(
            "Mehmet", "Demir", "mehmet@test.com",
            "HASHED_password", "05552222222",
            UserRole.Courier);

        var courierProfile = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 123");

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez", "Aziziye",
            "Düzce Merkez", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "Düzce", "Merkez", "Cedidiye",
            "Düzce Merkez", 40.8438, 31.1565);

        var shipment = new Shipment(
            "CT123456", sender.Id, receiver.Id,
            senderAddress.Id, receiverAddress.Id,
            5, DateTime.Now.AddDays(2));

        shipment.AssignWarehouse(warehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Önceden kurye ata.
        shipment.AssignDeliveryCourier(courierProfile.Id);

        await context.Users.AddRangeAsync(
            warehouseUser, sender, receiver, courierUser);

        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.CourierProfiles.AddAsync(courierProfile);
        await context.Addresses.AddRangeAsync(
            senderAddress, receiverAddress);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid> { courierProfile.Id }),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "A delivery courier has already been assigned.",
            exception.Message);

        Assert.Equal(
            ShipmentStatus.AssignedToCourier,
            shipment.Status);

        Assert.Equal(
            courierProfile.Id,
            shipment.DeliveryCourierId);
    }
    //Depo pasifse kurye ataması yapılamamalı.
    [Fact]
    public async Task Handle_InactiveWarehouse_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet", "Yılmaz", "ahmet@test.com",
            "HASHED_password", "05551111111",
            UserRole.WarehouseEmployee);

        var warehouse = new Warehouse(
            "Düzce Depo", "Düzce", "Merkez",
            "Sanayi Caddesi", 40.8438, 31.1565);

        warehouse.Deactivate();

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id, warehouse.Id);

        await context.Users.AddAsync(warehouseUser);
        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(Guid.NewGuid()),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "Shipment does not exist.",
            exception.Message);
    }
    //Yakında müsait kurye bulunamazsa atama yapılmamalı.
    [Fact]
    public async Task Handle_NoNearbyCouriersFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouseUser = new User(
            "Ahmet", "Yılmaz", "ahmet@test.com",
            "HASHED_password", "05551111111",
            UserRole.WarehouseEmployee);

        var warehouse = new Warehouse(
            "Düzce Depo", "Düzce", "Merkez",
            "Sanayi Caddesi", 40.8438, 31.1565);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseUser.Id, warehouse.Id);

        var sender = new User(
            "Veli", "Kaya", "veli@test.com",
            "HASHED_password", "05554444444",
            UserRole.Customer);

        var receiver = new User(
            "Ali", "Kaya", "ali@test.com",
            "HASHED_password", "05553333333",
            UserRole.Customer);

        var senderAddress = new Address(
            sender.Id, "Ev", "Düzce", "Merkez", "Aziziye",
            "Düzce Merkez", 40.8438, 31.1565);

        var receiverAddress = new Address(
            receiver.Id, "Ev", "Düzce", "Merkez", "Cedidiye",
            "Düzce Merkez", 40.8438, 31.1565);

        var shipment = new Shipment(
            "CT123456", sender.Id, receiver.Id,
            senderAddress.Id, receiverAddress.Id,
            5, DateTime.Now.AddDays(2));

        shipment.AssignWarehouse(warehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        await context.Users.AddRangeAsync(
            warehouseUser, sender, receiver);

        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Addresses.AddRangeAsync(senderAddress, receiverAddress);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseUser.Id
        };

        var handler = new AutoAssignDeliveryCourierCommandHandler(
            context,
            new FakeSender(new List<Guid>()),
            currentUserService);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(
                new AutoAssignDeliveryCourierCommand(shipment.Id),
                CancellationToken.None));

        // Assert
        Assert.Equal(
            "No available couriers found near the warehouse.",
            exception.Message);

        Assert.Null(shipment.DeliveryCourierId);
        Assert.Empty(context.OutboxMessages);
        Assert.Empty(context.ShipmentTrackingEvents);
    }
}
