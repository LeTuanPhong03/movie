using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Theaters.Create;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint quản lý phòng chiếu. Mặc định mọi action cần đăng nhập ([Authorize] trên class),
/// action ghi dữ liệu thêm policy AdminOnly.
/// </summary>
[ApiController]
[Route("api/theaters")]
[Authorize]
public class TheatersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TheaterDto>> Create(CreateTheaterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        // 201 + header Location chỉ tới phòng vừa tạo (endpoint GET theo id sẽ làm sau)
        return Created($"/api/theaters/{result.Id}", result);
    }
}
