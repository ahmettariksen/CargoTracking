using CargoTracking.API.Contracts.Addresses;
using CargoTracking.Application.Features.Addresses.Commands.CreateAddress;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AddressesController:ControllerBase
{
    private readonly ISender _sender;

    public AddressesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateAddressRequest request,CancellationToken cancellationToken)
    {
        var command = new CreateAddressCommand(
            request.UserId,
            request.Title,
            request.City,
            request.District,
            request.Neighborhood,
            request.FullAddress,
            request.Latitude,
            request.Longitude);

        var addressId = await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, addressId);
    }
}
