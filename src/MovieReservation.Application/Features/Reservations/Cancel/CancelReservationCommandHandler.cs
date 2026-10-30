using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Cancel;

/// <summary>
/// Huỷ đơn nguyên tử: một UPDATE có điều kiện (đúng đơn, chủ đơn hoặc Admin, Pending/Confirmed,
/// suất chưa chiếu) nên khi Huỷ/Xác nhận/Job dọn chạy cùng lúc trên một đơn chỉ một bên sửa được, bên sau nhận 0 dòng.
/// 0 dòng thì hỏi lại "người gọi có được thấy đơn không": không → 404 (không lộ đơn của người khác), có → 409.
/// Ghế nhả về Available lọc theo ReservationId của chính đơn này, nên không đụng ghế người khác đã giữ lại.
/// </summary>
public class CancelReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CancelReservationCommand>
{
    public async Task Handle(CancelReservationCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        int donDaHuy = await db.Reservations
            .Where(r => r.Id == request.ReservationId
                     && (r.UserId == request.UserId || request.IsAdmin)
                     && (r.Status == ReservationStatus.Pending || r.Status == ReservationStatus.Confirmed)
                     && db.Showtimes.Any(s => s.Id == r.ShowtimeId && s.StartAt > now))
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ReservationStatus.Cancelled)
                .SetProperty(r => r.CancelledAt, now), cancellationToken);
        if (donDaHuy == 0)
        {
            // Cùng điều kiện "được thấy" như dòng UPDATE: không tồn tại hoặc của người khác đều → false → 404
            bool duocPhepThay = await db.Reservations
                .AnyAsync(r => r.Id == request.ReservationId
                            && (r.UserId == request.UserId || request.IsAdmin), cancellationToken);
            if (!duocPhepThay)
                throw new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");
            throw new ConflictException("Đơn đã huỷ hoặc đã hết hạn, hoặc suất chiếu đã bắt đầu.");
        }
        await db.ShowtimeSeats
            .Where(ss => ss.ReservationId == request.ReservationId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ss => ss.Status, SeatStatus.Available)
                .SetProperty(ss => ss.ReservationId, (int?)null)
                .SetProperty(ss => ss.HeldUntil, (DateTime?)null), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
