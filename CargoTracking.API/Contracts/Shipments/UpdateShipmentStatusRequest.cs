using CargoTracking.Domain.Enums;

namespace CargoTracking.API.Contracts.Shipments;

public sealed record UpdateShipmentStatusRequest(
    ShipmentStatus NewStatus,
    string Description);
