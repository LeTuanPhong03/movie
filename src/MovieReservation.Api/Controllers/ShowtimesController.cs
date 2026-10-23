using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Showtimes.Create;
using MovieReservation.Application.Features.Showtimes.DTOs;
using MovieReservation.Application.Features.Showtimes.GetByDate;
using MovieReservation.Application.Features.Showtimes.GetSeats;
using MovieReservation.Application.Features.Showtimes.GetShowtimeById;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint suất chiếu. Mặc định chỉ Admin (AdminOnly trên class, giống MoviesController);
/// các action xem suất chiếu cho khách (Bài 11) sẽ mở bằng [AllowAnonymous].
/// </summary>
[ApiController]
[Route("api/showtimes")]
[Authorize(Policy = "AdminOnly")]
public class ShowtimesController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<ShowtimeDto>>> GetByDate([FromQuery] GetShowtimesByDateQuery query, CancellationToken cancellationToken)
    {
        var showtimes = await sender.Send(query, cancellationToken);
        return Ok(showtimes);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<ShowtimeDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetShowtimeByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // Sơ đồ ghế là tài nguyên con của suất chiếu: /api/showtimes/{id}/seats
    [HttpGet("{id:int}/seats")]
    [AllowAnonymous]
    public async Task<ActionResult<SeatMapDto>> GetSeats(int id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetSeatsByShowtimeQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ShowtimeDto>> Create(CreateShowtimeCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
}
