using CargoTracking.API.Contracts.Couriers;
using CargoTracking.API.Services;
using CargoTracking.Application.Features.Couriers.Commands.CreateCourier;
using CargoTracking.Application.Features.Couriers.Commands.UpdateLocation;
using CargoTracking.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CouriersController : ControllerBase
{
    private readonly ISender _sender;

    public CouriersController(ISender sender)
    {
        _sender = sender;
    }
    [HttpPost]
    [Authorize(Roles ="Admin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]

    // Bazı controller metotlarına parametre olarak DTO yapısı bazende direkt command sınıfını veriyoruz sebebi aşağıda belittim
    // API istemcisinden yalnızca gerekli kurye bilgilerini alarak
    // API sözleşmesini Application katmanındaki Command yapısından bağımsız tutar.
    public async Task<ActionResult<Guid>> Create([FromBody] CreateCourierRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateCourierCommand(
      request.FirstName,
      request.LastName,
      request.Email,
      request.Password,
      request.PhoneNumber,
      request.VehicleType,
      request.VehiclePlate);

        var courierId = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, courierId);
    }

    [HttpPut("location")]
    [Authorize(Roles = "Courier")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateCourierLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCourierLocationCommand(
       request.Latitude,
       request.Longitude);

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}
