using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reports.Capacity;

/// <summary>
/// Đếm ghế theo trạng thái của từng suất ngay trong SQL (COUNT có điều kiện), không tải ShowtimeSeat về.
/// Lấy cả suất đã chiếu và phim đã ngừng chiếu — báo cáo nhìn lại quá khứ. Ghế Held đã quá HeldUntil
/// tính là trống (hết hạn lười, giống lúc đặt vé), nên số liệu đúng cả khi job dọn chưa chạy.
/// </summary>
public class GetCapacityReportQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetCapacityReportQuery, CapacityReportDto>
{
    public async Task<CapacityReportDto> Handle(GetCapacityReportQuery request, CancellationToken cancellationToken)
    {
        var vn = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var from = TimeZoneInfo.ConvertTimeToUtc(request.From.ToDateTime(TimeOnly.MinValue), vn);
        var to = TimeZoneInfo.ConvertTimeToUtc(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), vn);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var counts = await db.Showtimes
            .AsNoTracking()
            .Where(s => s.StartAt >= from && s.StartAt < to)
            .OrderBy(s => s.StartAt)
            .ThenBy(s => s.Id)
            .Select(s => new
            {
                s.Id,
                s.MovieId,
                s.Movie.Title,
                TheaterName = s.Theater.Name,
                s.StartAt,
                Total = s.ShowtimeSeats.Count(),
                Booked = s.ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Booked),
                Held = s.ShowtimeSeats.Count(ss => ss.Status == SeatStatus.Held && ss.HeldUntil > now)
            })
            .ToListAsync(cancellationToken);

        var rows = counts.Select(c => new CapacityRowDto(
            c.Id, c.MovieId, c.Title, c.TheaterName, c.StartAt,
            TotalSeats: c.Total,
            BookedSeats: c.Booked,
            HeldSeats: c.Held,
            AvailableSeats: c.Total - c.Booked - c.Held,
            OccupancyPercent: Percent(c.Booked, c.Total))).ToList();

        var totalSeats = rows.Sum(r => r.TotalSeats);
        var bookedSeats = rows.Sum(r => r.BookedSeats);
        return new CapacityReportDto(request.From, request.To, totalSeats, bookedSeats, Percent(bookedSeats, totalSeats), rows);
    }

    // Phòng 0 ghế không xảy ra (validator tạo phòng), nhưng chặn chia cho 0 vẫn rẻ hơn một lỗi 500
    private static double Percent(int part, int total) => total == 0 ? 0 : Math.Round(part * 100.0 / total, 1);
}
