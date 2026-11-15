using CargoTracking.Domain.Common;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Domain.Entities;
public sealed class ShipmentTrackingEvent : BaseEntity
{
    // Kargo durum geçmişini tutacağımız entity(tablo)
    public Guid ShipmentId { get; private set; }
    public ShipmentStatus Status { get; private set; }
    public string Description { get; private set; } = default!;
    public Guid? WarehouseId { get; private set; }
    public Guid? CourierId { get; private set; }
    public Shipment Shipment { get; private set; } = default!;
    public Warehouse? Warehouse { get; private set; }
    public CourierProfile? Courier { get; private set; }
    private ShipmentTrackingEvent()
    {
    }
    public ShipmentTrackingEvent(
        Guid shipmentId,
        ShipmentStatus status,
        string description,
        Guid? warehouseId = null,
        Guid? courierId = null)
    {
        ShipmentId = shipmentId;
        Status = status;
        Description = description;
        WarehouseId = warehouseId;
        CourierId = courierId;
    }
}
