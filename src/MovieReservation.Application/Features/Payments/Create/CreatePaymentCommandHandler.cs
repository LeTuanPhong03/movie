using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Payments.Create;

/// <summary>
/// Tạo một lần thanh toán (Payment Pending) và link VNPay cho đơn. Hạn thanh toán gửi cho VNPay = ExpiresAt của đơn,
/// KHÔNG kéo dài hạn giữ ghế: kéo dài thì ai cũng giữ ghế mãi được bằng cách bấm "thanh toán" liên tục
/// (docs/decisions.md mục 7). Còn dưới MinRemainingSeconds thì từ chối luôn vì không kịp nhập thẻ.
/// Số tiền lấy từ TotalPrice trong DB, không bao giờ nhận từ client.
/// </summary>
public class CreatePaymentCommandHandler(IAppDbContext db, IVnPayGateway gateway, TimeProvider timeProvider)
    : IRequestHandler<CreatePaymentCommand, PaymentLinkDto>
{
    public const int MinRemainingSeconds = 60;

    public async Task<PaymentLinkDto> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var reservation = await db.Reservations
            .AsNoTracking()
            .Where(r => r.Id == request.ReservationId && r.UserId == request.UserId)
            .Select(r => new { r.Status, r.ExpiresAt, r.TotalPrice })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");

        if (reservation.Status != ReservationStatus.Pending)
            throw new ConflictException("Đơn không còn chờ thanh toán (đã thanh toán, đã huỷ hoặc đã hết hạn).");
        if (reservation.ExpiresAt <= now.AddSeconds(MinRemainingSeconds))
            throw new ConflictException("Đơn đã hết hoặc sắp hết hạn giữ ghế, vui lòng đặt lại.");

        var payment = new Payment
        {
            ReservationId = request.ReservationId,
            // Guid thay vì ghép giờ + Id: 2 lần bấm trong cùng mili giây vẫn không trùng (unique index sẽ ném lỗi 500)
            TxnRef = Guid.NewGuid().ToString("N"),
            Amount = reservation.TotalPrice,
            Status = PaymentStatus.Pending,
            CreatedAt = now,
        };
        // Tạo link TRƯỚC khi lưu: thiếu cấu hình VNPay thì lỗi ngay, không để lại dòng Payment Pending rác
        var url = gateway.CreatePaymentUrl(new VnPayPaymentRequest(
            TxnRef: payment.TxnRef,
            Amount: payment.Amount,
            OrderInfo: $"Thanh toan don ve {request.ReservationId}",
            ClientIp: request.ClientIp,
            CreatedAtUtc: now,
            ExpiresAtUtc: reservation.ExpiresAt));

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);

        return new PaymentLinkDto(url, reservation.ExpiresAt);
    }
}
