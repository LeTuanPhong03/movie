using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Confirm;

/// <summary>
/// Xác nhận đơn NGUYÊN TỬ: một câu UPDATE có điều kiện (đúng đơn, đúng chủ, còn Pending, chưa hết hạn)
/// thay cho "đọc → kiểm tra trong C# → SaveChanges" (check-then-act: người khác chen vào giữa được).
/// DB đếm số dòng bị sửa: 1 = thành công, 0 = không đủ điều kiện. Sau đó chuyển ghế Held → Booked;
/// cả 2 câu nằm trong một transaction để không bao giờ có đơn Confirmed mà ghế vẫn Held.
/// </summary>
public class ConfirmReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ConfirmReservationCommand>
{
    public async Task Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // 1. Đổi đơn sang Confirmed — mọi điều kiện nằm trong WHERE nên DB kiểm và sửa trong cùng một bước.
        // ExpiresAt > now: lazy expiry không ghi DB, đơn quá hạn vẫn đang Pending → phải tự chặn ở đây.
        int soDonDaSua = await db.Reservations
            .Where(r => r.Id == request.ReservationId
                     && r.UserId == request.UserId
                     && r.Status == ReservationStatus.Pending
                     && r.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReservationStatus.Confirmed)
                .SetProperty(r => r.ConfirmedAt, now), cancellationToken);

        if (soDonDaSua == 0)
        {
            // 0 dòng chưa nói được VÌ SAO → đọc thêm để trả đúng lỗi.
            // Đơn của người khác cũng trả 404 (không phải 403) để không lộ "đơn số này có tồn tại" (chống IDOR).
            bool laDonCuaMinh = await db.Reservations
                .AnyAsync(r => r.Id == request.ReservationId && r.UserId == request.UserId, cancellationToken);
            if (!laDonCuaMinh)
                throw new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");
            throw new ConflictException("Đơn đã hết hạn giữ ghế, đã thanh toán hoặc đã huỷ.");
        }

        // 2. Chuyển ghế của đơn sang Booked. Lọc theo ReservationId (lớp phòng thủ 2): ghế đã sang đơn khác thì không đụng tới.
        // ExecuteUpdate cũng đổi xmin của ghế → ai đang đặt ghế này bằng SaveChanges sẽ nhận DbUpdateConcurrencyException.
        int soGheDaSua = await db.ShowtimeSeats
            .Where(ss => ss.ReservationId == request.ReservationId && ss.Status == SeatStatus.Held)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ss => ss.Status, SeatStatus.Booked)
                .SetProperty(ss => ss.HeldUntil, (DateTime?)null), cancellationToken);

        if (soGheDaSua == 0)
            // Không commit → transaction tự rollback khi dispose → đơn quay về Pending
            throw new ConflictException("Ghế của đơn không còn được giữ, không thể xác nhận.");

        await transaction.CommitAsync(cancellationToken);
    }
}
