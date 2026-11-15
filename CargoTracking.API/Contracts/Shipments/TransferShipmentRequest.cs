namespace CargoTracking.API.Contracts.Shipments;

public sealed record TransferShipmentRequest(
    Guid TargetWarehouseId);
