using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Expire;

/// <summary>
/// Dọn đơn quá hạn: đơn Pending đã qua ExpiresAt → Expired, ghế của các đơn đó → Available.
/// Hai câu UPDATE nằm trong một transaction để không bao giờ có đơn Expired mà ghế vẫn Held.
/// An toàn khi chạy cùng lúc với Confirm: cả hai đều chỉ sửa đơn còn Pending, DB khoá dòng nên
/// bên đến sau thấy đơn đã đổi trạng thái và sửa 0 dòng → một đơn chỉ có thể Confirmed hoặc Expired.
/// </summary>
public class ExpireReservationsCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<ExpireReservationsCommand, int>
{
    public async Task<int> Handle(ExpireReservationsCommand request, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // 1. Đổi đơn sang Expired — mọi điều kiện nằm trong WHERE nên DB kiểm và sửa trong cùng một bước.
        int soDonDaSua = await db.Reservations
            .Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.Status, ReservationStatus.Expired), cancellationToken);

        // 2. Nhả ghế: chỉ ghế trỏ tới đơn ĐÃ Expired. Ghế mà người khác đã lấy lại (nhờ lazy expiry)
        // trỏ tới đơn mới còn Pending nên không khớp điều kiện → không bị nhả nhầm.
        // ExecuteUpdate cũng đổi xmin của ghế → ai đang đặt ghế này bằng SaveChanges sẽ nhận DbUpdateConcurrencyException.
        await db.ShowtimeSeats
            .Where(ss => ss.Status == SeatStatus.Held && ss.Reservation!.Status == ReservationStatus.Expired)
            .ExecuteUpdateAsync(s => s
                .SetProperty(ss => ss.Status, SeatStatus.Available)
                .SetProperty(ss => ss.HeldUntil, (DateTime?)null)
                .SetProperty(ss => ss.ReservationId, (int?)null), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return soDonDaSua;
    }
}
