using Azure;
using CargoTracking.Application.Features.Shipments.Commands.ReceiveShipmentAtWarehouse;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using CargoTracking.Application.Interfaces;
using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using CargoTracking.Tests.Fakes;
using CargoTracking.Tests.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CargoTracking.Tests.Features.Shipments.Commands.ReceiveShipmentAtWarehouse;
public sealed class ReceiveShipmentAtWarehouseCommandHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ShouldReceiveShipmentAtWarehouse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouse = CreateWarehouse();

        var warehouseEmployee = new User(
    "Test",
    "Employee",
    "employee@test.com",
    "hashedPassword",
    "5551112233",
    UserRole.WarehouseEmployee);

        var employeeProfile = new WareHouseEmployeeProfile(
    warehouseEmployee.Id,
    warehouse.Id);

        var shipment = CreateShipment();
        var pickupCourierId = Guid.NewGuid();

        var courierUser = new User(
    "Test",
    "Courier",
    "courier@test.com",
    "hashedPassword",
    "5551113344",
    UserRole.Courier);

        var courierProfile = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "81 ABC 123");

        shipment.AssignPickupCourier(courierProfile.Id);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.MarkPickedUpFromSender();

        await context.Users.AddAsync(warehouseEmployee);
        await context.Users.AddAsync(courierUser);
        await context.CourierProfiles.AddAsync(courierProfile);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Shipments.AddAsync(shipment);
        await context.Warehouses.AddAsync(warehouse);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(
            shipment.Id);
        //warehouse.Id);

        var nearestWarehouse = WarehouseResponse(warehouse);
        var fakeSender = new FakeWarehouseSender(nearestWarehouse);
        var userId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler =
            new ReceiveShipmentAtWarehouseCommandHandler(context, fakeSender, currentUserService);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
        Assert.Equal(warehouse.Id, shipment.CurrentWarehouseId);

        var trackingEvent = await context.ShipmentTrackingEvents
            .SingleOrDefaultAsync(x => x.ShipmentId == shipment.Id);

        Assert.NotNull(trackingEvent);
        Assert.Equal(ShipmentStatus.AtWarehouse, trackingEvent.Status);
        Assert.Equal(warehouse.Id, trackingEvent.WarehouseId);
        Assert.Equal(
            "Shipment received at warehouse.",
            trackingEvent.Description);
    }

    [Fact]
    public async Task Handle_ShipmentDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var warehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            warehouse.Id);

        await context.Users.AddAsync(warehouseEmployee);
        await context.Warehouses.AddAsync(warehouse);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(Guid.NewGuid());

        var nearestWarehouse = WarehouseResponse(warehouse);
        var fakeSender = new FakeWarehouseSender(nearestWarehouse);

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("Shipment does not exist.", exception.Message);

        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    [Fact]
    public async Task Handle_WarehouseDoesNotExist_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateAcceptedShipment();
        var warehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            warehouse.Id);

        await context.Users.AddAsync(warehouseEmployee);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Shipments.AddAsync(shipment);
        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        var nearestWarehouse = WarehouseResponse(warehouse);
        var fakeSender = new FakeWarehouseSender(nearestWarehouse);

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Shipment cannot be received at warehouse.",
            exception.Message);

        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);
        Assert.Null(shipment.CurrentWarehouseId);

        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    //[Fact]
    //public async Task Handle_InactiveWarehouse_ShouldThrowException()
    //{
    //    // Arrange
    //    await using var context = TestDbContextFactory.Create();

    //    var shipment = CreateAcceptedShipment();
    //    shipment.ChangeStatus(ShipmentStatus.PickedUpFromSender);

    //    var warehouse = CreateWarehouse();
    //    warehouse.SetActive(false);

    //    await context.Shipments.AddAsync(shipment);
    //    await context.Warehouses.AddAsync(warehouse);
    //    await context.SaveChangesAsync();

    //    var command = new ReceiveShipmentAtWarehouseCommand(
    //        shipment.Id);

    //    var nearestWarehouse = WarehouseResponse(warehouse);
    //    var fakeSender = new FakeWarehouseSender(nearestWarehouse);

    //    var handler = new ReceiveShipmentAtWarehouseCommandHandler(
    //        context,
    //        fakeSender);

    //    // Act
    //    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
    //        () => handler.Handle(command, CancellationToken.None));

    //    // Assert
    //    Assert.Equal(
    //        "Warehouse does not exist or is inactive.",
    //        exception.Message);

    //    Assert.Equal(
    //        ShipmentStatus.PickedUpFromSender,
    //        shipment.Status);

    //    Assert.Null(shipment.CurrentWarehouseId);

    //    Assert.Empty(
    //        await context.ShipmentTrackingEvents.ToListAsync());
    //}

    [Fact]
    public async Task Handle_ShipmentNotPickedUpFromSender_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateShipment();
        var warehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            warehouse.Id);

        await context.Users.AddAsync(warehouseEmployee);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Shipments.AddAsync(shipment);
        await context.Warehouses.AddAsync(warehouse);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        var nearestWarehouse = WarehouseResponse(warehouse);
        var fakeSender = new FakeWarehouseSender(nearestWarehouse);

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "Shipment cannot be received at warehouse.",
            exception.Message);

        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Null(shipment.CurrentWarehouseId);

        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }
    [Fact]
    public async Task Handle_InactiveWarehouse_ShouldNotReturnInactiveWarehouse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var shipment = CreateShipment();
        var warehouse = CreateWarehouse();
        warehouse.SetActive(false);

        await context.Shipments.AddAsync(shipment);
        await context.Warehouses.AddAsync(warehouse);
        await context.SaveChangesAsync();

        // Act
        var warehouses = await context.Warehouses
            .Where(x => x.IsActive)
            .ToListAsync();

        // Assert
        Assert.DoesNotContain(
            warehouses,
            x => x.Id == warehouse.Id);
    }
    [Fact]
    public async Task Handle_InTransitShipment_ShouldReceiveAtTargetWarehouse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var currentWarehouse = CreateWarehouse();
        var targetWarehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            targetWarehouse.Id);

        var shipment = CreateShipment();

        // Kargo kabul edilir.
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        // Kargo ilk depoya alınır.
        shipment.AssignWarehouse(currentWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Hedef depo belirlenir.
        shipment.AssingTargetWarehouse(targetWarehouse.Id);

        // Depolar arası transfer başlatılır.
        shipment.ChangeStatus(ShipmentStatus.InTransit);

        await context.Users.AddAsync(warehouseEmployee);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Warehouses.AddAsync(currentWarehouse);
        await context.Warehouses.AddAsync(targetWarehouse);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        var fakeSender = new FakeWarehouseSender(
            WarehouseResponse(targetWarehouse));

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
        Assert.Equal(targetWarehouse.Id, shipment.CurrentWarehouseId);
        Assert.Equal(targetWarehouse.Id, shipment.TargetWarehouseId);

        var trackingEvent = await context.ShipmentTrackingEvents
            .SingleOrDefaultAsync(x => x.ShipmentId == shipment.Id);

        Assert.NotNull(trackingEvent);
        Assert.Equal(ShipmentStatus.AtWarehouse, trackingEvent.Status);
        Assert.Equal(targetWarehouse.Id, trackingEvent.WarehouseId);
        Assert.Equal(
            "Shipment received at warehouse.",
            trackingEvent.Description);
    }

    // Bu testte özellikle kargonun durumunun ve depo bilgilerinin hata sonrasında değişmediğini de doğruluyoruz.
    [Fact]
    public async Task Handle_InTransitShipment_WrongWarehouse_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var currentWarehouse = CreateWarehouse();
        var targetWarehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        // Çalışan hedef depo yerine mevcut depoya bağlı.
        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            currentWarehouse.Id);

        var shipment = CreateShipment();

        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.AssignWarehouse(currentWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);
        shipment.AssingTargetWarehouse(targetWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.InTransit);

        await context.Users.AddAsync(warehouseEmployee);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Warehouses.AddAsync(currentWarehouse);
        await context.Warehouses.AddAsync(targetWarehouse);
        await context.Shipments.AddAsync(shipment);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        var fakeSender = new FakeWarehouseSender(
            WarehouseResponse(targetWarehouse));

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal(
            "You can only receive shipments at your assigned target warehouse.",
            exception.Message);

        Assert.Equal(ShipmentStatus.InTransit, shipment.Status);
        Assert.Equal(currentWarehouse.Id, shipment.CurrentWarehouseId);
        Assert.Equal(targetWarehouse.Id, shipment.TargetWarehouseId);

        Assert.Empty(await context.ShipmentTrackingEvents.ToListAsync());
    }

    // Başarısız teslimat sonrası kargonun mevcut depoya geri kabul edilmesi.
    [Fact]
    public async Task Handle_DeliveryFailedShipment_ShouldReceiveAtCurrentWarehouse()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var firstWarehouse = CreateWarehouse();
        var currentWarehouse = CreateWarehouse();

        var warehouseEmployee = new User(
            "Test",
            "Employee",
            "employee@test.com",
            "hashedPassword",
            "5551112233",
            UserRole.WarehouseEmployee);

        var courierUser = new User(
    "Ahmet",
    "Yılmaz",
    "courier@test.com",
    "hashedPassword",
    "05551111111",
    UserRole.Courier);
        var courierProfile = new CourierProfile(
    courierUser.Id,
    VehicleType.Motorcycle,
    "34 ABC 123");
        courierProfile.SetAvailability(false);

        var employeeProfile = new WareHouseEmployeeProfile(
            warehouseEmployee.Id,
            currentWarehouse.Id);

        var shipment = CreateShipment();

        // Kargo ilk depoya ulaşır.
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.AssignWarehouse(firstWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Kargo ikinci depoya transfer edilir.
        shipment.AssingTargetWarehouse(currentWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.InTransit);

        shipment.CompleteWarehouseTransfer();
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Kargo teslimat için kuryeye atanır.
        shipment.AssignDeliveryCourier(courierProfile.Id);

        // Kurye kargoyu alır ancak teslim edemez.
        shipment.ChangeStatus(ShipmentStatus.PickedUp);
        shipment.ChangeStatus(ShipmentStatus.DeliveryFailed);

        await context.Users.AddAsync(warehouseEmployee);
        await context.WareHouseEmployeeProfiles.AddAsync(employeeProfile);
        await context.Warehouses.AddAsync(firstWarehouse);
        await context.Warehouses.AddAsync(currentWarehouse);
        await context.Shipments.AddAsync(shipment);
        context.Users.Add(courierUser);
        context.CourierProfiles.Add(courierProfile);

        await context.SaveChangesAsync();

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        var fakeSender = new FakeWarehouseSender(
            WarehouseResponse(currentWarehouse));

        var currentUserService = new FakeCurrentUserService
        {
            UserId = warehouseEmployee.Id
        };

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            fakeSender,
            currentUserService);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);

        Assert.Equal(
            currentWarehouse.Id,
            shipment.CurrentWarehouseId);

        var trackingEvent = await context.ShipmentTrackingEvents
            .SingleOrDefaultAsync(x => x.ShipmentId == shipment.Id);

        Assert.NotNull(trackingEvent);

        Assert.Equal(
            ShipmentStatus.AtWarehouse,
            trackingEvent.Status);

        Assert.Equal(
            currentWarehouse.Id,
            trackingEvent.WarehouseId);

        Assert.Equal(
            "Shipment received at warehouse.",
            trackingEvent.Description);
        Assert.True(courierProfile.IsAvailable);
    }
    //yanlış depodaki çalışanın DeliveryFailed durumundaki kargoyu teslim almaya çalışmasını test edeceğiz
    [Fact]
    public async Task Handle_DeliveryFailedShipmentReceivedByWrongWarehouse_ShouldThrowException()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        var firstWarehouse = CreateWarehouse();
        var currentWarehouse = CreateWarehouse();

        var employeeUser = new User(
            "Mehmet",
            "Demir",
            "employee@test.com",
            "hashedPassword",
            "05552222222",
            UserRole.WarehouseEmployee);

        // Çalışan yanlış depoya bağlı.
        var employeeProfile = new WareHouseEmployeeProfile(
            employeeUser.Id,
            firstWarehouse.Id);

        var courierUser = new User(
            "Ahmet",
            "Yılmaz",
            "courier@test.com",
            "hashedPassword",
            "05551111111",
            UserRole.Courier);

        var courierProfile = new CourierProfile(
            courierUser.Id,
            VehicleType.Motorcycle,
            "34 ABC 123");

        courierProfile.SetAvailability(false);

        var shipment = CreateShipment();

        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.AssignWarehouse(firstWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        shipment.AssingTargetWarehouse(currentWarehouse.Id);
        shipment.ChangeStatus(ShipmentStatus.InTransit);

        shipment.CompleteWarehouseTransfer();
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        shipment.AssignDeliveryCourier(courierProfile.Id);
        shipment.ChangeStatus(ShipmentStatus.PickedUp);
        shipment.ChangeStatus(ShipmentStatus.DeliveryFailed);

        context.Warehouses.AddRange(firstWarehouse, currentWarehouse);
        context.Users.AddRange(employeeUser, courierUser);
        context.WareHouseEmployeeProfiles.Add(employeeProfile);
        context.CourierProfiles.Add(courierProfile);
        context.Shipments.Add(shipment);

        await context.SaveChangesAsync();

        var currentUserService = new FakeCurrentUserService
        {
            UserId = employeeUser.Id
        };

        var sender = new FakeWarehouseSender(
            WarehouseResponse(currentWarehouse));

        var handler = new ReceiveShipmentAtWarehouseCommandHandler(
            context,
            sender,
            currentUserService);

        var command = new ReceiveShipmentAtWarehouseCommand(shipment.Id);

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => handler.Handle(command, CancellationToken.None));

        // Assert
        Assert.Equal(
            "You can only receive returned shipments at their current warehouse.",
            exception.Message);

        Assert.Equal(ShipmentStatus.DeliveryFailed, shipment.Status);
        Assert.Equal(currentWarehouse.Id, shipment.CurrentWarehouseId);
        Assert.False(courierProfile.IsAvailable);

        Assert.Empty(
            context.ShipmentTrackingEvents.Where(x => x.ShipmentId == shipment.Id));
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

    private static Shipment CreateAcceptedShipment()
    {
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        return shipment;
    }

    private static Warehouse CreateWarehouse()
    {
        return new Warehouse(
            "Düzce Merkez Depo",
            "Düzce",
            "Merkez",
            "Örnek depo adresi",
            40.8438,
            31.1565);
    }
    private static NearestWarehouseResponse WarehouseResponse(Warehouse warehouse)
    {
        return new NearestWarehouseResponse(
            warehouse.Id,
            warehouse.Name,
            warehouse.City,
            warehouse.District,
            warehouse.Address,
            warehouse.Latitude,
            warehouse.Longitude);

    }
}
