using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Application.Features.Auth.Login;
using MovieReservation.Application.Features.Auth.Logout;
using MovieReservation.Application.Features.Auth.Refresh;
using MovieReservation.Application.Features.Auth.Register;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint xác thực: đăng ký, đăng nhập, làm mới token, đăng xuất. Không endpoint nào cần access token.
/// Controller chỉ nhận request và chuyển cho MediatR, không chứa logic nghiệp vụ.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
