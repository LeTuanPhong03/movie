using System.Net;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Payments.Create;
using MovieReservation.Application.Features.Reservations.Cancel;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Application.Features.Reservations.GetById;
using MovieReservation.Application.Features.Reservations.GetMine;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Endpoint đặt vé, bắt buộc đăng nhập. UserId luôn đọc từ token (đã ký, không giả được);
/// body/query dùng record riêng không có userId để Swagger không gợi ý gửi userId lên.
/// POST /confirm là thanh toán giả, chỉ bật khi Reservations:AllowManualConfirm = true (Development);
/// production phải đi qua VNPay (POST /payments) — không thì ai cũng confirm được mà không trả tiền.
/// </summary>
[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController(ISender sender, IConfiguration configuration) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationDto>>> GetMine([FromQuery] MyReservationsRequest request, CancellationToken cancellationToken)
    {
        var query = new GetMyReservationsQuery(UserId, request.Status, request.Page, request.PageSize);
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReservationDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var query = new GetReservationByIdQuery(id, UserId, User.IsInRole("Admin"));
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ReservationDto>> Create(CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateReservationCommand(UserId, request.ShowtimeId, request.SeatIds);
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/payments")]
    public async Task<ActionResult<PaymentLinkDto>> CreatePayment(int id, CancellationToken cancellationToken)
    {
        // ::1 (localhost IPv6).MapToIPv4() ra "0.0.0.1" chứ không phải 127.0.0.1 → xử lý loopback riêng
        var ip = HttpContext.Connection.RemoteIpAddress;
        var clientIp = ip is null || IPAddress.IsLoopback(ip) ? "127.0.0.1" : ip.MapToIPv4().ToString();
        return Ok(await sender.Send(new CreatePaymentCommand(id, UserId, clientIp), cancellationToken));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id, CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Reservations:AllowManualConfirm"))
            return NotFound();
        await sender.Send(new ConfirmReservationCommand(id, UserId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        // Role đọc từ claim ở đây (Api biết token), handler chỉ nhận bool
        var isAdmin = User.IsInRole("Admin");
        await sender.Send(new CancelReservationCommand(id, UserId, isAdmin), cancellationToken);
        return NoContent();
    }
}

public record CreateReservationRequest(int ShowtimeId, List<int> SeatIds);

public record MyReservationsRequest(ReservationStatus? Status = null, int Page = 1, int PageSize = 10);
