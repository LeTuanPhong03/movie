using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieReservation.Application.Features.Payments.VnPayCallback;

namespace MovieReservation.Api.Controllers;

/// <summary>
/// Hai cửa VNPay gọi về, đều [AllowAnonymous] vì không có JWT — thứ chứng minh "đúng là VNPay" là chữ ký HMAC
/// kiểm trong handler. IPN: server VNPay gọi, phải trả JSON {RspCode, Message} đúng định dạng, không thì VNPay gọi lại.
/// Return: trình duyệt khách quay về (chưa có FE thì VnPay:ReturnUrl trỏ thẳng vào đây; có FE thì trang kết quả của FE
/// chuyển nguyên query string sang đây). Xử lý luôn nếu IPN chưa tới — chạy local không có URL public thì IPN không bao giờ tới.
/// </summary>
[ApiController]
[Route("api/payments/vnpay")]
[AllowAnonymous]
public class PaymentsController(ISender sender) : ControllerBase
{
    [HttpGet("ipn")]
    public async Task<IActionResult> Ipn(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ProcessVnPayCallbackCommand(ReadQuery()), cancellationToken);
        return Ok(new { result.RspCode, result.Message });
    }

    [HttpGet("return")]
    public async Task<ActionResult<VnPayCallbackResult>> Return(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ProcessVnPayCallbackCommand(ReadQuery()), cancellationToken));
    }

    private Dictionary<string, string> ReadQuery() =>
        Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
}
