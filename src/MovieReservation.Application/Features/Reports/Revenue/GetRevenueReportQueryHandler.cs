using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reports.Revenue;

/// <summary>
/// Doanh thu = tổng TotalPrice (giá chụp lúc đặt, không phải giá suất hiện tại) của đơn đang Confirmed,
/// lọc theo ConfirmedAt (lúc thu tiền). Đơn Confirmed rồi bị huỷ có Status = Cancelled → tự rơi khỏi báo cáo
/// (coi như đã hoàn tiền); Pending/Expired chưa thu đồng nào nên không tính.
/// Ngày VN đổi thành khoảng UTC [from, to) để lọc (giống GetShowtimesByDate); nhóm theo ngày thì cộng 7 giờ
/// rồi lấy phần ngày — Việt Nam không đổi giờ mùa hè nên +7 luôn đúng. GROUP BY chạy trong SQL, không tải từng đơn về.
/// </summary>
public class GetRevenueReportQueryHandler(IAppDbContext db) : IRequestHandler<GetRevenueReportQuery, RevenueReportDto>
{
    private const int VietnamUtcOffsetHours = 7;

    public async Task<RevenueReportDto> Handle(GetRevenueReportQuery request, CancellationToken cancellationToken)
    {
        var vn = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var from = TimeZoneInfo.ConvertTimeToUtc(request.From.ToDateTime(TimeOnly.MinValue), vn);
        var to = TimeZoneInfo.ConvertTimeToUtc(request.To.AddDays(1).ToDateTime(TimeOnly.MinValue), vn);

        var confirmed = db.Reservations
            .AsNoTracking()
            .Where(r => r.Status == ReservationStatus.Confirmed
                     && r.ConfirmedAt >= from && r.ConfirmedAt < to);

        List<RevenueRowDto> rows;
        if (request.GroupBy == RevenueGroupBy.Day)
        {
            var byDay = await confirmed
                .GroupBy(r => r.ConfirmedAt!.Value.AddHours(VietnamUtcOffsetHours).Date)
                .Select(g => new
                {
                    Day = g.Key,
                    Revenue = g.Sum(r => r.TotalPrice),
                    Reservations = g.Count(),
                    Tickets = g.Sum(r => r.Seats.Count)
                })
                .OrderBy(x => x.Day)
                .ToListAsync(cancellationToken);
            rows = byDay.Select(x => new RevenueRowDto(x.Day.ToString("yyyy-MM-dd"), x.Revenue, x.Reservations, x.Tickets)).ToList();
        }
        else
        {
            var byMovie = await confirmed
                .GroupBy(r => new { r.Showtime.MovieId, r.Showtime.Movie.Title })
                .Select(g => new
                {
                    g.Key.MovieId,
                    g.Key.Title,
                    Revenue = g.Sum(r => r.TotalPrice),
                    Reservations = g.Count(),
                    Tickets = g.Sum(r => r.Seats.Count)
                })
                .OrderByDescending(x => x.Revenue)
                .ThenBy(x => x.Title)
                .ThenBy(x => x.MovieId)
                .ToListAsync(cancellationToken);
            rows = byMovie.Select(x => new RevenueRowDto(x.Title, x.Revenue, x.Reservations, x.Tickets, x.MovieId)).ToList();
        }

        return new RevenueReportDto(
            request.From, request.To, request.GroupBy,
            TotalRevenue: rows.Sum(r => r.Revenue),
            TotalReservations: rows.Sum(r => r.Reservations),
            TotalTickets: rows.Sum(r => r.Tickets),
            Rows: rows);
    }
}
