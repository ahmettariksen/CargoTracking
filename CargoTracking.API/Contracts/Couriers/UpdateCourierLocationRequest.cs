namespace CargoTracking.API.Contracts.Couriers;

public sealed record UpdateCourierLocationRequest(
     double Latitude,
    double Longitude);

