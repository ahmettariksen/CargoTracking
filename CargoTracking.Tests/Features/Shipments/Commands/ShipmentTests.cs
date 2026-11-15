using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Shipments.Commands;
public sealed class ShipmentTests
{
    private static Shipment CreateShipment()
    {
        return new Shipment(
            trackingNumber: "TR123456",
            senderId: Guid.NewGuid(),
            receiverId: Guid.NewGuid(),
            senderAddressId: Guid.NewGuid(),
            receiverAddressId: Guid.NewGuid(),
            weight: 2.5m,
            estimatedDeliveryDate: DateTime.Now.AddDays(3));
    }

    // PICKUP COURIER

    [Fact]
    public void AssignPickupCourier_CreatedShipment_ShouldAssignCourierAndChangeStatus()
    {
        // Arrange
        var shipment = CreateShipment();
        var courierId = Guid.NewGuid();

        // Act
        shipment.AssignPickupCourier(courierId);

        // Assert
        Assert.Equal(courierId, shipment.PickupCourierId);
        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }

    [Fact]
    public void AssignPickupCourier_EmptyCourierId_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => shipment.AssignPickupCourier(Guid.Empty));
    }

    [Fact]
    public void AssignPickupCourier_WrongStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Cancelled);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.AssignPickupCourier(Guid.NewGuid()));
    }

    [Fact]
    public void AssignPickupCourier_AlreadyAssigned_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.AssignPickupCourier(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.AssignPickupCourier(Guid.NewGuid()));
    }

    // PICKUP FROM SENDER

    [Fact]
    public void MarkPickedUpFromSender_WithAssignedCourier_ShouldChangeStatus()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.AssignPickupCourier(Guid.NewGuid());
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        // Act
        shipment.MarkPickedUpFromSender();

        // Assert
        Assert.Equal(
            ShipmentStatus.PickedUpFromSender,
            shipment.Status);
    }
    [Fact]
    public void MarkPickedUpFromSender_WithoutAssignedCourier_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            shipment.MarkPickedUpFromSender);
    }

    // DELIVERY COURIER

    [Fact]
    public void AssignDeliveryCourier_AtWarehouseShipment_ShouldAssignCourierAndChangeStatus()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        var courierId = Guid.NewGuid();

        // Act
        shipment.AssignDeliveryCourier(courierId);

        // Assert
        Assert.Equal(courierId, shipment.DeliveryCourierId);
        Assert.Equal(ShipmentStatus.AssignedToCourier, shipment.Status);
    }

    [Fact]
    public void AssignDeliveryCourier_EmptyCourierId_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => shipment.AssignDeliveryCourier(Guid.Empty));
    }

    [Fact]
    public void AssignDeliveryCourier_WrongStatus_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.AssignDeliveryCourier(Guid.NewGuid()));
    }

    [Fact]
    public void AssignDeliveryCourier_AlreadyAssigned_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);
        shipment.AssignDeliveryCourier(Guid.NewGuid());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.AssignDeliveryCourier(Guid.NewGuid()));
    }

    [Fact]
    public void AssignDeliveryCourier_AfterAssignment_StatusShouldBeAssignedToCourier()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Act
        shipment.AssignDeliveryCourier(Guid.NewGuid());

        // Assert
        Assert.Equal(ShipmentStatus.AssignedToCourier, shipment.Status);
    }

    // STATUS VALIDATION

    [Fact]
    public void ChangeStatus_PickedUpWithoutDeliveryCourier_ShouldThrowException()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.ChangeStatus(ShipmentStatus.PickedUp));
    }
}
