using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.GetByDate;

/// <summary>
/// Lấy danh sách suất chiếu của một ngày cho khách. DB lưu UTC nên đổi ngày Việt Nam thành khoảng UTC [from, to) rồi mới lọc;
/// chỉ lấy suất chưa bắt đầu và phim đang chiếu, sắp theo giờ bắt đầu, Select thẳng sang ShowtimeDto.
/// </summary>
public class GetShowtimesByDateQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetShowtimesByDateQuery, List<ShowtimeDto>>
{
    public async Task<List<ShowtimeDto>> Handle(GetShowtimesByDateQuery request, CancellationToken cancellationToken)
    {
        // ── Việc 1: đổi "ngày VN" khách gửi thành cái hộp [from, to) theo UTC ──
        var vn = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var from = TimeZoneInfo.ConvertTimeToUtc(request.Date.ToDateTime(TimeOnly.MinValue), vn);
        var to = from.AddDays(1);

        // ── Việc 2: "bây giờ" theo UTC ──
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // ── Việc 3: truy vấn ──
        var showtimes = await db.Showtimes
            .AsNoTracking()
            .Where(s => s.StartAt >= from && s.StartAt < to && s.StartAt > now && s.Movie.IsActive)
            .OrderBy(s => s.StartAt)
            .Select(s => new ShowtimeDto(
                Id: s.Id,
                Title: s.Movie.Title,
                StartAt: s.StartAt,
                EndAt: s.EndAt,
                TheaterName: s.Theater.Name,
                Price: s.Price))
            .ToListAsync(cancellationToken);

        return showtimes;
    }
}
