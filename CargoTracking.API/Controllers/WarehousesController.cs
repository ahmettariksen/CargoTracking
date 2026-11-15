using CargoTracking.API.Contracts.Shipments;
using CargoTracking.Application.Features.Warehouses.Commands.CreateWarehouse;
using CargoTracking.Application.Features.Warehouses.Commands.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WarehousesController : ControllerBase
{
    private readonly ISender _sender;

    public WarehousesController(ISender sender)
    {
        _sender = sender;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateWarehouseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateWarehouseCommand(
            request.Name,
            request.City,
            request.District,
            request.Address,
            request.Latitude,
            request.Longitude);

        var warehouseId = await _sender.Send(
            command,
            cancellationToken);

        return Ok(warehouseId);
    }

    [Authorize]
    [HttpGet("nearest/{shipmentId:guid}")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    public async Task<ActionResult<Guid>> GetNearestWarehouse(Guid shipmentId,
    CancellationToken cancellationToken)
    {
        var warehouseId = await _sender.Send(
            new SelectNearestWarehouseQuery(shipmentId),
            cancellationToken);

        return Ok(warehouseId);
    }
}
