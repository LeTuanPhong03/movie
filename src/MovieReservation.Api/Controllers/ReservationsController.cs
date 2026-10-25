using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Features.Reservations.DTOs;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint đặt vé, bắt buộc đăng nhập. UserId luôn đọc từ token (đã ký, không giả được);
/// body dùng CreateReservationRequest không có userId để Swagger không gợi ý gửi userId lên.
/// </summary>
[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreateReservationCommand(userId, request.ShowtimeId, request.SeatIds);
        var result = await sender.Send(command, cancellationToken);
        // Chưa có GET /api/reservations/{id} nên chưa dùng CreatedAtAction được; có endpoint đó thì đổi
        return StatusCode(StatusCodes.Status201Created, result);
    }
}

public record CreateReservationRequest(int ShowtimeId, List<int> SeatIds);
