using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Application.Features.Auth.Login;
using MovieReservation.Application.Features.Auth.Register;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint xác thực: đăng ký  đăng nhập ( sau thêm logout, làm mới token).
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

}
