using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Theaters.Create;
using MovieReservation.Application.Features.Theaters.DTOs;
using MovieReservation.Application.Features.Theaters.GetAll;
using MovieReservation.Application.Features.Theaters.GetById;

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
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<List<TheaterDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetTheatersQuery(), cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TheaterDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetTheaterByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<TheaterDto>> Create(CreateTheaterCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        // 201 + header Location sinh từ route của GetById → đổi route thì link vẫn đúng
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
