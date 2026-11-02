using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Confirm;

/// <summary>
/// Hai câu UPDATE xác nhận đơn dùng chung cho ConfirmReservationCommandHandler (khách bấm confirm)
/// và ProcessVnPayCallbackCommandHandler (VNPay báo đã trả tiền). KHÔNG tự mở transaction:
/// người gọi mở, vì IPN còn phải cập nhật Payment trong cùng transaction đó.
/// userId = null nghĩa là không lọc theo chủ đơn (IPN không có người đăng nhập — tin VNPay nhờ chữ ký).
/// </summary>
public static class ReservationConfirmer
{
    /// <returns>true nếu đã xác nhận; false nếu đơn không đủ điều kiện (không phải Pending, hết hạn, sai chủ).</returns>
    public static async Task<bool> TryConfirmAsync(
        IAppDbContext db, int reservationId, int? userId, DateTime now, CancellationToken cancellationToken)
    {
        // 1. Đổi đơn sang Confirmed — mọi điều kiện nằm trong WHERE nên DB kiểm và sửa trong cùng một bước.
        // ExpiresAt > now: lazy expiry không ghi DB, đơn quá hạn vẫn đang Pending → phải tự chặn ở đây.
        int soDonDaSua = await db.Reservations
            .Where(r => r.Id == reservationId
                     && (userId == null || r.UserId == userId)
                     && r.Status == ReservationStatus.Pending
                     && r.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReservationStatus.Confirmed)
                .SetProperty(r => r.ConfirmedAt, now), cancellationToken);
        if (soDonDaSua == 0)
            return false;

        // 2. Chuyển ghế của đơn sang Booked. Lọc theo ReservationId (lớp phòng thủ 2): ghế đã sang đơn khác thì không đụng tới.
        // ExecuteUpdate cũng đổi xmin của ghế → ai đang đặt ghế này bằng SaveChanges sẽ nhận DbUpdateConcurrencyException.
        int soGheDaSua = await db.ShowtimeSeats
            .Where(ss => ss.ReservationId == reservationId && ss.Status == SeatStatus.Held)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ss => ss.Status, SeatStatus.Booked)
                .SetProperty(ss => ss.HeldUntil, (DateTime?)null), cancellationToken);
        if (soGheDaSua == 0)
            // Người gọi không commit → transaction rollback khi dispose → đơn quay về Pending
            throw new ConflictException("Ghế của đơn không còn được giữ, không thể xác nhận.");
        return true;
    }
}
