using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Controller xử lý các endpoint liên quan đến tài khoản người dùng.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
{
    Id = User.FindFirstValue(ClaimTypes.NameIdentifier),
    Email = User.FindFirstValue(ClaimTypes.Email),
    Role = User.FindFirstValue(ClaimTypes.Role)
});
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok(new { Message = "Chỉ admin mới có thể truy cập endpoint này." });
    
}
