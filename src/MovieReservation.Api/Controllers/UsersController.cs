using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Application.Features.Users.ChangeRole;
using MovieReservation.Application.Features.Users.GetUsers;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Quản lý user cho Admin: tìm user và đổi quyền (đề bài: Admin nâng user lên Admin).
/// Ví dụ: PUT /api/users/5/role với body { "role": "Admin" }.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public class UsersController(ISender sender) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpPut("{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, ChangeRoleRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeUserRoleCommand(id, request.Role, CurrentUserId), cancellationToken);
        return NoContent();
    }
}

public record ChangeRoleRequest(UserRole Role);
