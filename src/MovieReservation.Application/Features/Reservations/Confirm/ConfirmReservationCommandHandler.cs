using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;

namespace MovieReservation.Application.Features.Reservations.Confirm;

/// <summary>
/// Xác nhận đơn NGUYÊN TỬ: một câu UPDATE có điều kiện (đúng đơn, đúng chủ, còn Pending, chưa hết hạn)
/// thay cho "đọc → kiểm tra trong C# → SaveChanges" (check-then-act: người khác chen vào giữa được).
/// DB đếm số dòng bị sửa: 1 = thành công, 0 = không đủ điều kiện. Sau đó chuyển ghế Held → Booked;
/// cả 2 câu (nằm trong ReservationConfirmer, dùng chung với IPN VNPay) chạy trong một transaction
/// để không bao giờ có đơn Confirmed mà ghế vẫn Held.
/// </summary>
public class ConfirmReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ConfirmReservationCommand>
{
    public async Task Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        bool daXacNhan = await ReservationConfirmer.TryConfirmAsync(
            db, request.ReservationId, request.UserId, now, cancellationToken);

        if (!daXacNhan)
        {
            // 0 dòng chưa nói được VÌ SAO → đọc thêm để trả đúng lỗi.
            // Đơn của người khác cũng trả 404 (không phải 403) để không lộ "đơn số này có tồn tại" (chống IDOR).
            bool laDonCuaMinh = await db.Reservations
                .AnyAsync(r => r.Id == request.ReservationId && r.UserId == request.UserId, cancellationToken);
            if (!laDonCuaMinh)
                throw new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");
            throw new ConflictException("Đơn đã hết hạn giữ ghế, đã thanh toán hoặc đã huỷ.");
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
