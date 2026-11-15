using CargoTracking.Domain.Common;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class Shipment : BaseEntity
{
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();
    public string TrackingNumber { get; private set; } = default!;

    public Guid SenderId { get; private set; }
    public Guid ReceiverId { get; private set; }

    public Guid SenderAddressId { get; private set; }
    public Guid ReceiverAddressId { get; private set; }

    public Guid? CurrentWarehouseId { get; private set; }
    public Guid? TargetWarehouseId { get; private set; }

    // nullable çünkü kargo ilk oluşturulduğunda henüz kurye atanmış olmayabilir.
    // Göndericiden kargoyu alan kurye.
    public Guid? PickupCourierId { get; private set; }

    // Depodan alıp son teslimatı gerçekleştiren kurye.
    public Guid? DeliveryCourierId { get; private set; }

    // Başarısız teslimat sonrası göndericiye iade yapacak kurye.
    public Guid? ReturnCourierId { get; private set; }

    public ShipmentStatus Status { get; private set; }

    public decimal Weight { get; private set; }

    public DateTime? EstimatedDeliveryDate { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    public User Sender { get; private set; } = default!;
    public User Receiver { get; private set; } = default!;
    public Address SenderAddress { get; private set; } = default!;
    public Address ReceiverAddress { get; private set; } = default!;
    public Warehouse? CurrentWarehouse { get; private set; }
    public Warehouse? TargetWarehouse { get; private set; }
    public CourierProfile? PickupCourier { get; private set; }
    public CourierProfile? DeliveryCourier { get; private set; }
    public CourierProfile? ReturnCourier { get; private set; }
    private Shipment()
    {
    }
    public Shipment(
       string trackingNumber,
       Guid senderId,
       Guid receiverId,
       Guid senderAddressId,
       Guid receiverAddressId,
       decimal weight,
       DateTime? estimatedDeliveryDate)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            throw new ArgumentException(
                "Tracking number cannot be empty.",
                nameof(trackingNumber));

        if (senderId == Guid.Empty)
            throw new ArgumentException(
                "Sender ID cannot be empty.",
                nameof(senderId));

        if (receiverId == Guid.Empty)
            throw new ArgumentException(
                "Receiver ID cannot be empty.",
                nameof(receiverId));

        if (senderId == receiverId)
            throw new ArgumentException(
                "Sender and receiver cannot be the same user.");

        if (senderAddressId == Guid.Empty)
            throw new ArgumentException(
                "Sender address ID cannot be empty.",
                nameof(senderAddressId));

        if (receiverAddressId == Guid.Empty)
            throw new ArgumentException(
                "Receiver address ID cannot be empty.",
                nameof(receiverAddressId));

        if (weight <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(weight),
                "Weight must be greater than zero.");

        if (estimatedDeliveryDate.HasValue &&
            estimatedDeliveryDate.Value < DateTime.Now)
        {
            throw new ArgumentException(
                "Estimated delivery date cannot be in the past.",
                nameof(estimatedDeliveryDate));
        }
        TrackingNumber = trackingNumber;
        SenderId = senderId;
        ReceiverId = receiverId;
        SenderAddressId = senderAddressId;
        ReceiverAddressId = receiverAddressId;
        Weight = weight;
        EstimatedDeliveryDate = estimatedDeliveryDate;
        Status = ShipmentStatus.Created;
    }
    public void AssignPickupCourier(Guid courierId)
    {
        if (courierId == Guid.Empty)
            throw new ArgumentException(
                "Courier ID cannot be empty.",
                nameof(courierId));

        if (Status != ShipmentStatus.Created)
            throw new InvalidOperationException(
                "A pickup courier can only be assigned to an accepted shipment.");

        if (PickupCourierId.HasValue)
            throw new InvalidOperationException(
                "A pickup courier has already been assigned.");

        PickupCourierId = courierId;
        UpdatedAt = DateTime.Now;
    }
    public void MarkPickedUpFromSender()
    {
        //. Bu metot, kargonun göndericiden gerçekten alındığını kaydedecek:
        if (Status != ShipmentStatus.Accepted)
            throw new InvalidOperationException(
                "Shipment must be accepted before pickup.");

        if (!PickupCourierId.HasValue)
            throw new InvalidOperationException(
                "A pickup courier must be assigned first.");

        ChangeStatus(ShipmentStatus.PickedUpFromSender);
    }
    public void AssignDeliveryCourier(Guid courierId)
    {
        if (courierId == Guid.Empty)
            throw new ArgumentException(
                "Courier ID cannot be empty.",
                nameof(courierId));

        if (DeliveryCourierId.HasValue)
            throw new InvalidOperationException(
                "A delivery courier has already been assigned.");

        if (Status != ShipmentStatus.AtWarehouse)
            throw new InvalidOperationException(
                "A delivery courier can only be assigned when the shipment is at a warehouse.");

        DeliveryCourierId = courierId;

        ChangeStatus(ShipmentStatus.AssignedToCourier);
    }
    public void AssignReturnCourier(Guid courierId)
    {
        if (courierId == Guid.Empty)
            throw new ArgumentException(
                "Courier ID cannot be empty.",
                nameof(courierId));

        if (Status != ShipmentStatus.AtWarehouse)
            throw new InvalidOperationException(
                "A return courier can only be assigned when the shipment is at a warehouse.");

        if (ReturnCourierId.HasValue)
            throw new InvalidOperationException(
                "A return courier has already been assigned.");

        ReturnCourierId = courierId;
        ChangeStatus(ShipmentStatus.AssignedToCourier);
    }
    public void AssignWarehouse(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException(
                "Warehouse ID cannot be empty.",
                nameof(warehouseId));

        CurrentWarehouseId = warehouseId;
        UpdatedAt = DateTime.Now;
    }
    public void AssingTargetWarehouse(Guid warehouseId)
    {
        if (warehouseId == Guid.Empty)
            throw new ArgumentException("Warehouse ID cannot be empty.", nameof(warehouseId));

        if (CurrentWarehouseId == warehouseId)
            throw new InvalidOperationException("Target warehouse cannot be the current warehouse.");

        TargetWarehouseId = warehouseId;
        UpdatedAt = DateTime.Now;
    }

    public void CompleteWarehouseTransfer()
    {
        //Transfer tamamlandığında hedef depoyu mevcut depoya aktarmak için.
        if (TargetWarehouseId is null)
            throw new InvalidOperationException(
                "Target warehouse has not been assigned.");

        CurrentWarehouseId = TargetWarehouseId;
        //TargetWarehouseId = null;
        UpdatedAt = DateTime.Now;
    }
    public void ChangeStatus(ShipmentStatus newStatus)
    {
        if (!IsValidStatusTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
               $"Invalid shipment status transition: {Status} -> {newStatus}");
        }
       // if (newStatus == ShipmentStatus.PickedUp &&
       //DeliveryCourierId is null)
       // {
       //     throw new InvalidOperationException(
       //         "A delivery courier must be assigned before pickup.");
       // }
       // Status = newStatus;
       // UpdatedAt = DateTime.Now;

        if (newStatus == ShipmentStatus.PickedUp)
        {
            bool hasAssignedCourier =
                (Status == ShipmentStatus.AssignedToCourier &&
                 (DeliveryCourierId is not null || ReturnCourierId is not null));

            if (!hasAssignedCourier)
            {
                throw new InvalidOperationException(
                    "A courier must be assigned before pickup.");
            }
        }
        Status = newStatus;
        UpdatedAt = DateTime.Now;

        if (newStatus == ShipmentStatus.Delivered)
        {
            DeliveredAt = DateTime.Now;
        }
    }
    public void CompleteReturn()
    {
        if (Status != ShipmentStatus.PickedUp)
            throw new InvalidOperationException(
                "Shipment must be picked up before completing return.");

        if (ReturnCourierId is null)
            throw new InvalidOperationException(
                "A return courier must be assigned.");

        ChangeStatus(ShipmentStatus.Returned);
    }
    private static bool IsValidStatusTransition(
       ShipmentStatus currentStatus,
       ShipmentStatus newStatus)
    {
        return currentStatus switch
        {
            ShipmentStatus.Created =>
                newStatus == ShipmentStatus.Accepted ||
                newStatus == ShipmentStatus.Cancelled,

            ShipmentStatus.Accepted =>
                newStatus == ShipmentStatus.PickedUpFromSender ||
                newStatus == ShipmentStatus.AtWarehouse ||
                newStatus == ShipmentStatus.Cancelled,

            ShipmentStatus.PickedUpFromSender =>
                newStatus == ShipmentStatus.AtWarehouse,

            ShipmentStatus.AtWarehouse =>
                newStatus == ShipmentStatus.AssignedToCourier ||
                newStatus == ShipmentStatus.InTransit ||
                newStatus == ShipmentStatus.Cancelled,

            ShipmentStatus.AssignedToCourier =>
                newStatus == ShipmentStatus.PickedUp,

            ShipmentStatus.PickedUp =>
                newStatus == ShipmentStatus.InTransit ||
                newStatus == ShipmentStatus.Delivered ||
                  newStatus == ShipmentStatus.DeliveryFailed||
                  newStatus == ShipmentStatus.Returned,

            ShipmentStatus.InTransit =>
                newStatus == ShipmentStatus.AtWarehouse||
                newStatus==ShipmentStatus.Delivered,
            //newStatus == ShipmentStatus.OutForDelivery,

            //ShipmentStatus.OutForDelivery =>
            //    newStatus == ShipmentStatus.Delivered ||
            //    newStatus == ShipmentStatus.DeliveryFailed,

            ShipmentStatus.DeliveryFailed =>
                //newStatus == ShipmentStatus.OutForDelivery ||
                // kargo göndericiye geri iade olunca returned olacak
                //newStatus == ShipmentStatus.Returned,
            newStatus == ShipmentStatus.AtWarehouse,

            ShipmentStatus.Delivered => false,
            ShipmentStatus.Cancelled => false,
            ShipmentStatus.Returned => false,

            _ => false
        };
    }
}
