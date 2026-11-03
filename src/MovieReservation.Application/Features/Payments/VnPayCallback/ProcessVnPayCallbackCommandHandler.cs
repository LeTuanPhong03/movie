using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Payments.VnPayCallback;

/// <summary>
/// Thứ tự kiểm tra là thứ tự tin tưởng: (1) chữ ký HMAC — sai thì dừng NGAY, chưa đọc gì từ DB, vì ai cũng gọi được URL này;
/// (2) TxnRef có thật; (3) số tiền khớp (chống sửa số tiền); (4) Payment còn Pending.
/// Idempotent nhờ UPDATE Payments ... WHERE Status = 'Pending': VNPay gọi lại / return URL chạy song song thì
/// chỉ một bên sửa được 1 dòng, bên kia 0 dòng → "02". Cập nhật Payment và xác nhận đơn chung một transaction.
/// Tiền đã trừ mà đơn không còn xác nhận được (hết hạn/huỷ đúng lúc đó) → Payment RefundRequired, vẫn trả "00"
/// </summary>
public class ProcessVnPayCallbackCommandHandler(
    IAppDbContext db,
    IVnPayGateway gateway,
    TimeProvider timeProvider,
    ILogger<ProcessVnPayCallbackCommandHandler> logger)
    : IRequestHandler<ProcessVnPayCallbackCommand, VnPayCallbackResult>
{
    public async Task<VnPayCallbackResult> Handle(ProcessVnPayCallbackCommand request, CancellationToken cancellationToken)
    {
        var p = request.Parameters;
        if (!gateway.IsValidSignature(p))
            return new VnPayCallbackResult("97", "Invalid signature");

        var txnRef = p.GetValueOrDefault("vnp_TxnRef");
        var payment = await db.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TxnRef == txnRef, cancellationToken);
        if (payment is null)
            return new VnPayCallbackResult("01", "Order not found");

        if (!long.TryParse(p.GetValueOrDefault("vnp_Amount"), NumberStyles.None, CultureInfo.InvariantCulture, out var amount)
            || amount != (long)(payment.Amount * 100))
            return new VnPayCallbackResult("04", "Invalid amount", payment.ReservationId, payment.Status);

        if (payment.Status != PaymentStatus.Pending)
            return new VnPayCallbackResult("02", "Order already confirmed", payment.ReservationId, payment.Status);

        // Cả hai mã đều "00" mới là trừ tiền thành công (vnp_ResponseCode = kết quả, vnp_TransactionStatus = trạng thái giao dịch)
        bool daTraTien = p.GetValueOrDefault("vnp_ResponseCode") == "00" && p.GetValueOrDefault("vnp_TransactionStatus") == "00";
        var now = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var trangThaiMoi = daTraTien ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        int soDong = await db.Payments
            .Where(x => x.Id == payment.Id && x.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, trangThaiMoi)
                .SetProperty(x => x.ProviderTransactionNo, p.GetValueOrDefault("vnp_TransactionNo"))
                .SetProperty(x => x.ResponseCode, p.GetValueOrDefault("vnp_ResponseCode"))
                .SetProperty(x => x.CompletedAt, now), cancellationToken);
        if (soDong == 0)
            // Một lời gọi khác (IPN lặp / return URL) vừa xử lý xong giữa lúc mình đọc và lúc mình sửa
            return new VnPayCallbackResult("02", "Order already confirmed", payment.ReservationId);

        if (daTraTien)
        {
            bool daXacNhan = await ReservationConfirmer.TryConfirmAsync(
                db, payment.ReservationId, userId: null, now, cancellationToken);
            if (!daXacNhan)
            {
                trangThaiMoi = PaymentStatus.RefundRequired;
                await db.Payments
                    .Where(x => x.Id == payment.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, PaymentStatus.RefundRequired), cancellationToken);
                logger.LogWarning(
                    "Đơn {ReservationId} đã được trả tiền (TxnRef {TxnRef}) nhưng không còn xác nhận được → cần hoàn tiền",
                    payment.ReservationId, payment.TxnRef);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return new VnPayCallbackResult("00", "Confirm Success", payment.ReservationId, trangThaiMoi);
    }
}
