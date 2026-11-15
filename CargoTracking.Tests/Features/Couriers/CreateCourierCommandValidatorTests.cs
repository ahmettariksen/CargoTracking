using CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
using CargoTracking.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CargoTracking.Tests.Features.Couriers;
public sealed class CreateCourierCommandValidatorTests
{
    private readonly CreateCourierCommandValidator _validator = new();

    [Fact]
    public async Task Validate_EmptyFirstName_ShouldFail()
    {
        var command = new CreateCourierCommand(
            "",
            "Demir",
            "mehmet@example.com",
            "StrongPass123!",
            "05551234567",
            VehicleType.Motorcycle,
            "81 DEF 456");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "FirstName");
    }

    [Fact]
    public async Task Validate_InvalidEmail_ShouldFail()
    {
        var command = new CreateCourierCommand(
            "Mehmet",
            "Demir",
            "invalid-email",
            "StrongPass123!",
            "05551234567",
            VehicleType.Motorcycle,
            "81 DEF 456");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Email");
    }

    [Fact]
    public async Task Validate_ShortPassword_ShouldFail()
    {
        var command = new CreateCourierCommand(
            "Mehmet",
            "Demir",
            "mehmet@example.com",
            "123",
            "05551234567",
            VehicleType.Motorcycle,
            "81 DEF 456");

        var result = await _validator.ValidateAsync(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == "Password");
    }
}
