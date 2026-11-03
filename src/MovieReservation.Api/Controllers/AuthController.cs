using MediatR;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Api.Auth;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Application.Features.Auth.Login;
using MovieReservation.Application.Features.Auth.Logout;
using MovieReservation.Application.Features.Auth.Refresh;
using MovieReservation.Application.Features.Auth.Register;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint xác thực: đăng ký, đăng nhập, làm mới token, đăng xuất. Không endpoint nào cần access token.
/// Refresh token đi qua cookie HttpOnly (RefreshTokenCookie), body chỉ trả access token — đọc/ghi cookie là việc
/// của HTTP nên nằm ở controller; handler vẫn chỉ nhận chuỗi token như cũ, không biết cookie là gì.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(ISender sender, IConfiguration configuration) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<SessionResponse>> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return StartSession(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<SessionResponse>> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return StartSession(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<SessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        // Không có cookie = chưa đăng nhập (hoặc đã đăng xuất / cookie hết hạn) → 401, FE hiểu là khách
        var refreshToken = RefreshTokenCookie.Read(Request);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Unauthorized();
        }

        var result = await sender.Send(new RefreshTokenCommand(refreshToken), cancellationToken);
        return StartSession(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = RefreshTokenCookie.Read(Request);
        if (!string.IsNullOrEmpty(refreshToken))
        {
            await sender.Send(new LogoutCommand(refreshToken), cancellationToken);
        }

        RefreshTokenCookie.Delete(Response);
        return NoContent();
    }

    // Rotation: mỗi lần cấp phiên là một refresh token mới → ghi đè cookie cũ
    private OkObjectResult StartSession(AuthResponse result)
    {
        RefreshTokenCookie.Append(Response, result.RefreshToken, configuration.GetValue<int>("Jwt:RefreshTokenDays"));
        return Ok(SessionResponse.From(result));
    }
}
