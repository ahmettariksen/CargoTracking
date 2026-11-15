using CargoTracking.Domain.Enums;

namespace CargoTracking.API.Contracts.Couriers;

public sealed record CreateCourierRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string PhoneNumber,
    VehicleType VehicleType,
    string VehiclePlate);
