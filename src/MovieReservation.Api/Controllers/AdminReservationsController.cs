using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.GetAll;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Admin xem tất cả đơn đặt. Tách controller riêng (không nhét vào ReservationsController) vì ở đó
/// mọi action làm việc với đơn của chính người gọi; trộn vào dễ quên policy AdminOnly cho một action.
/// Ví dụ: GET /api/admin/reservations?showtimeId=3&amp;status=Confirmed&amp;page=1
/// </summary>
[ApiController]
[Route("api/admin/reservations")]
[Authorize(Policy = "AdminOnly")]
public class AdminReservationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminReservationDto>>> GetAll([FromQuery] GetAllReservationsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }
}
