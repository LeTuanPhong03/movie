using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Cancel;

/// <summary>
/// Huỷ đơn nguyên tử: một UPDATE có điều kiện (đúng đơn, chủ đơn hoặc Admin, Pending/Confirmed, suất chưa chiếu;
/// vé đã thanh toán thì khách phải huỷ trước giờ chiếu ít nhất Reservation.PaidCancelCutoffMinutes, Admin không bị giới hạn)
/// nên khi Huỷ/Xác nhận/Job dọn/IPN chạy cùng lúc trên một đơn chỉ một bên sửa được, bên sau nhận 0 dòng.
/// 0 dòng thì hỏi lại "người gọi có được thấy đơn không": không → 404 (không lộ đơn của người khác), có → 409 kèm lý do.
/// Cùng transaction: nhả ghế (lọc theo ReservationId của chính đơn này) và chuyển Payment đã thu tiền sang RefundRequired
/// để tiền của khách không mất dấu. IPN tới sau khi huỷ thì ProcessVnPayCallback tự đánh RefundRequired.
/// </summary>
public class CancelReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CancelReservationCommand>
{
    public async Task Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hanChotHuyVeDaTra = now.AddMinutes(Reservation.PaidCancelCutoffMinutes);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        int donDaHuy = await db.Reservations
            .Where(r => r.Id == request.ReservationId
                     && (r.UserId == request.UserId || request.IsAdmin)
                     && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                     && db.Showtimes.Any(s => s.Id == r.ShowtimeId && s.StartAt > now
                                           && (r.Status == ReservationStatus.Pending || request.IsAdmin || s.StartAt > hanChotHuyVeDaTra)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReservationStatus.Cancelled)
                .SetProperty(r => r.CancelledAt, now), cancellationToken);
        if (donDaHuy == 0)
            throw await LyDoKhongHuyDuocAsync(request, now, cancellationToken);

        await db.ShowtimeSeats
            .Where(ss => ss.ReservationId == request.ReservationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ss => ss.Status, SeatStatus.Available)
                .SetProperty(ss => ss.ReservationId, (int?)null)
                .SetProperty(ss => ss.HeldUntil, (DateTime?)null), cancellationToken);
        await db.Payments
            .Where(p => p.ReservationId == request.ReservationId && p.Status == PaymentStatus.Succeeded)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.RefundRequired), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<Exception> LyDoKhongHuyDuocAsync(CancelReservationCommand request, DateTime now, CancellationToken cancellationToken)
    {
        // Cùng điều kiện "được thấy" như dòng UPDATE: không tồn tại hoặc của người khác đều → null → 404
        var don = await db.Reservations
            .Where(r => r.Id == request.ReservationId && (r.UserId == request.UserId || request.IsAdmin))
            .Select(r => new { r.Status, r.Showtime.StartAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (don is null)
            return new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");
        if (don.Status == ReservationStatus.Confirmed && don.StartAt > now)
            return new ConflictException(
                $"Vé đã thanh toán chỉ huỷ được trước giờ chiếu ít nhất {Reservation.PaidCancelCutoffMinutes / 60} giờ.");
        return new ConflictException("Đơn đã huỷ hoặc đã hết hạn, hoặc suất chiếu đã bắt đầu.");
    }
}
