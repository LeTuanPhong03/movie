using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Refunds.GetAll;
using MovieReservation.Application.Features.Refunds.MarkRefunded;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Admin xử lý tiền phải hoàn: GET /api/admin/refunds?status=RefundRequired để xem,
/// POST /api/admin/refunds/{paymentId}/refunded sau khi đã hoàn tay để đánh dấu.
/// </summary>
[ApiController]
[Route("api/admin/refunds")]
[Authorize(Policy = "AdminOnly")]
public class AdminRefundsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RefundDto>>> GetAll([FromQuery] GetRefundsQuery query, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(query, cancellationToken));
    }

    [HttpPost("{paymentId:int}/refunded")]
    public async Task<IActionResult> MarkRefunded(int paymentId, CancellationToken cancellationToken)
    {
        await sender.Send(new MarkRefundedCommand(paymentId), cancellationToken);
        return NoContent();
    }
}
