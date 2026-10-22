using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Showtimes.Create;
using MovieReservation.Application.Features.Showtimes.DTOs;

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
    [HttpPost]
    public async Task<ActionResult<ShowtimeDto>> Create(CreateShowtimeCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        // Chưa có GET theo id nên tạm gõ URL tay; có GetById thì đổi sang CreatedAtAction
        return Created($"/api/showtimes/{result.Id}", result);
    }
}
