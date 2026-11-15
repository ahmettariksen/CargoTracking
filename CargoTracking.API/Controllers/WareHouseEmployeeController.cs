using CargoTracking.Application.Features.WareHouseEmployees;
using CargoTracking.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Admin))]
public class WareHouseEmployeeController(ISender sender):ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateWarehouseEmployee(
        [FromBody] CreateWareHouseEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var employeeId = await sender.Send(
            command,
            cancellationToken);

        return CreatedAtAction(
            nameof(CreateWarehouseEmployee),
            new
            {
                EmployeeId = employeeId,
                Message = "Warehouse employee created successfully."
            });
    }
}
