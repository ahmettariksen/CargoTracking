namespace CargoTracking.API.Contracts.Shipments;

public sealed record CreateWarehouseRequest(
    string Name,
    string City,
    string District,
    string Address,
    double Latitude,
    double Longitude);

