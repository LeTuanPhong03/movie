using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Showtimes.DTOs;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Showtimes.GetSeats;

/// <summary>
/// Trả sơ đồ ghế của một suất: suất không tồn tại → NotFoundException (404), tránh trả danh sách rỗng gây hiểu nhầm.
/// Ghế sắp theo hàng rồi số ghế. Ghế Held đã quá HeldUntil được hiển thị là Available (hết hạn lười — tính lúc đọc, DB vẫn ghi Held).
/// </summary>
public class GetSeatsByShowtimeQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetSeatsByShowtimeQuery, SeatMapDto>
{
    public async Task<SeatMapDto> Handle(GetSeatsByShowtimeQuery request, CancellationToken cancellationToken)
    {
        // Chỉ cần biết có hay không → AnyAsync (SELECT EXISTS), không kéo cả dòng lên
        var exists = await db.Showtimes.AnyAsync(s => s.Id == request.ShowtimeId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Không tìm thấy suất chiếu có Id {request.ShowtimeId}.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;

        var seats = await db.ShowtimeSeats
            .AsNoTracking()
            .Where(ss => ss.ShowtimeId == request.ShowtimeId)
            .OrderBy(ss => ss.Seat.Row)
            .ThenBy(ss => ss.Seat.Number)
            .Select(ss => new SeatStatusDto(
                ss.Id,
                ss.Seat.Row,
                ss.Seat.Number,
                ss.Status == SeatStatus.Held && ss.HeldUntil <= now ? SeatStatus.Available : ss.Status))
            .ToListAsync(cancellationToken);

        return new SeatMapDto(request.ShowtimeId, seats);
    }
}
