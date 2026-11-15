using CargoTracking.Domain.Entities;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Domain.Shipments;
public sealed class ShipmentStatusTests
{
    private static Shipment CreateShipment()
    {
        return new Shipment(
            trackingNumber: "CT-TEST-001",
            senderId: Guid.NewGuid(),
            receiverId: Guid.NewGuid(),
            senderAddressId: Guid.NewGuid(),
            receiverAddressId: Guid.NewGuid(),
            weight: 2.5m,
            estimatedDeliveryDate: DateTime.Now.AddDays(3));
    }

    // Kurye atanmışsa göndericiden teslim alma başarılı olmalı.
    [Fact]
    public void MarkPickedUpFromSender_WhenPickupCourierIsAssigned_ShouldSucceed()
    {
        // Arrange
        var shipment = CreateShipment();
        var courierId = Guid.NewGuid();

        shipment.AssignPickupCourier(courierId);
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        // Act
        shipment.MarkPickedUpFromSender();

        // Assert
        Assert.Equal(ShipmentStatus.PickedUpFromSender, shipment.Status);
        Assert.Equal(courierId, shipment.PickupCourierId);
    }

    // Kurye atanmamışsa teslim alma başarısız olmalı.
    [Fact]
    public void MarkPickedUpFromSender_WhenPickupCourierIsNotAssigned_ShouldThrow()
    {
        // Arrange
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => shipment.MarkPickedUpFromSender());

        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);
    }

    // Created durumundan Accepted durumuna geçiş başarılı olmalı.
    [Fact]
    public void ChangeStatus_FromCreatedToAccepted_ShouldSucceed()
    {
        var shipment = CreateShipment();

        shipment.ChangeStatus(ShipmentStatus.Accepted);

        Assert.Equal(ShipmentStatus.Accepted, shipment.Status);
    }

    // Accepted durumundan AtWarehouse durumuna geçiş başarılı olmalı.
    [Fact]
    public void ChangeStatus_FromAcceptedToAtWarehouse_ShouldSucceed()
    {
        var shipment = CreateShipment();
        shipment.ChangeStatus(ShipmentStatus.Accepted);

        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        Assert.Equal(ShipmentStatus.AtWarehouse, shipment.Status);
    }

    // Created durumundan doğrudan AtWarehouse durumuna geçilememeli.
    [Fact]
    public void ChangeStatus_FromCreatedToAtWarehouse_ShouldThrow()
    {
        var shipment = CreateShipment();

        Assert.Throws<InvalidOperationException>(
            () => shipment.ChangeStatus(ShipmentStatus.AtWarehouse));

        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }

    // Delivered durumundaki kargo tekrar InTransit durumuna geçememeli.
    [Fact]
    public void ChangeStatus_FromDeliveredToInTransit_ShouldThrow()
    {
        var shipment = CreateShipment();

        var pickupCourierId = Guid.NewGuid();
        shipment.AssignPickupCourier(pickupCourierId);
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.MarkPickedUpFromSender();

        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        shipment.AssignDeliveryCourier(Guid.NewGuid());

        shipment.ChangeStatus(ShipmentStatus.PickedUp);
        shipment.ChangeStatus(ShipmentStatus.InTransit);
        //shipment.ChangeStatus(ShipmentStatus.OutForDelivery);
        shipment.ChangeStatus(ShipmentStatus.Delivered);

        Assert.Throws<InvalidOperationException>(
            () => shipment.ChangeStatus(ShipmentStatus.InTransit));

        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    // Kargo Delivered olduğunda DeliveredAt doldurulmalı.
    [Fact]
    public void ChangeStatus_ToDelivered_ShouldSetDeliveredAt()
    {
        var shipment = CreateShipment();

        shipment.AssignPickupCourier(Guid.NewGuid());
        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.MarkPickedUpFromSender();

        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        shipment.AssignDeliveryCourier(Guid.NewGuid());

        shipment.ChangeStatus(ShipmentStatus.PickedUp);
        shipment.ChangeStatus(ShipmentStatus.InTransit);
        //shipment.ChangeStatus(ShipmentStatus.OutForDelivery);
        shipment.ChangeStatus(ShipmentStatus.Delivered);

        Assert.NotNull(shipment.DeliveredAt);
        Assert.Equal(ShipmentStatus.Delivered, shipment.Status);
    }

    // Geçersiz durum geçişinde mevcut durum korunmalı.
    [Fact]
    public void ChangeStatus_WhenTransitionIsInvalid_ShouldKeepCurrentStatus()
    {
        var shipment = CreateShipment();

        Assert.Throws<InvalidOperationException>(
            () => shipment.ChangeStatus(ShipmentStatus.AtWarehouse));

        Assert.Equal(ShipmentStatus.Created, shipment.Status);
    }

    // AtWarehouse durumundaki kargoya teslimat kuryesi atanabilmeli.
    [Fact]
    public void AssignCourier_WhenShipmentIsAtWarehouse_ShouldSucceed()
    {
        var shipment = CreateShipment();
        var courierId = Guid.NewGuid();

        shipment.ChangeStatus(ShipmentStatus.Accepted);
        shipment.ChangeStatus(ShipmentStatus.AtWarehouse);

        shipment.AssignDeliveryCourier(courierId);

        Assert.Equal(ShipmentStatus.AssignedToCourier, shipment.Status);
        Assert.Equal(courierId, shipment.DeliveryCourierId);
    }

    // Created durumundaki kargoya teslimat kuryesi atanamamalı.
    [Fact]
    public void AssignCourier_WhenShipmentIsCreated_ShouldThrow()
    {
        var shipment = CreateShipment();
        var courierId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(
            () => shipment.AssignDeliveryCourier(courierId));

        Assert.Equal(ShipmentStatus.Created, shipment.Status);
        Assert.Null(shipment.DeliveryCourierId);
    }
}
