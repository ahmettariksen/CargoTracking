namespace CargoTracking.API.Contracts.Shipments;

public sealed record CreateShipmentRequest(
    //Guid SenderId,
    Guid ReceiverId,
    Guid SenderAddressId,
    Guid ReceiverAddressId,
    decimal Weight,
    DateTime? EstimatedDeliveryDate);
