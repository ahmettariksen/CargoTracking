using CargoTracking.API.Contracts.Auth;
using CargoTracking.Application.Features.Auth.Commands.Login;
using CargoTracking.Application.Features.Auth.Commands.Register;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CargoTracking.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<ActionResult<Guid>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
           request.FirstName,
           request.LastName,
           request.Email,
           request.Password,
           request.PhoneNumber);

        var userId = await _sender.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, userId);
    }
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(command, cancellationToken);

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-test")]
    public IActionResult AdminTest()
    {
        return Ok(new { Message = "Admin yetkilendirmesi başarılı." });
    }
    [AllowAnonymous]
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody]RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var response = await _sender.Send(
       command,
       cancellationToken);

        return Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
    [FromBody] LogoutCommand command,
    CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);

        return NoContent();
    }
}
