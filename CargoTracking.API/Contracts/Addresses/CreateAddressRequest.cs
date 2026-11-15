namespace CargoTracking.API.Contracts.Addresses;

public sealed record CreateAddressRequest(
    Guid UserId,
    string Title,
    string City,
    string District,
    string Neighborhood,
    string FullAddress,
    double Latitude,
    double Longitude);
