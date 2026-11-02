using MediatR;

namespace MovieReservation.Application.Features.Reports.Revenue;

/// <summary>
/// Báo cáo doanh thu cho Admin trong khoảng ngày [From, To] (cả hai đầu, theo giờ Việt Nam),
/// nhóm theo ngày xác nhận hoặc theo phim. Chỉ tính đơn đang Confirmed (xem handler).
/// </summary>
public record GetRevenueReportQuery(DateOnly From, DateOnly To, RevenueGroupBy GroupBy = RevenueGroupBy.Day)
    : IRequest<RevenueReportDto>
{
    public const int MaxDays = 366;
}

public enum RevenueGroupBy
{
    Day,
    Movie
}

public record RevenueReportDto(
    DateOnly From,
    DateOnly To,
    RevenueGroupBy GroupBy,
    decimal TotalRevenue,
    int TotalReservations,
    int TotalTickets,
    List<RevenueRowDto> Rows);

/// <param name="Key">"yyyy-MM-dd" khi nhóm theo ngày, tên phim khi nhóm theo phim.</param>
/// <param name="MovieId">Chỉ có khi nhóm theo phim: tên phim không unique nên 2 phim trùng tên là 2 dòng cùng Key,
/// client phải phân biệt (và dùng làm React key) bằng MovieId.</param>
public record RevenueRowDto(string Key, decimal Revenue, int Reservations, int Tickets, int? MovieId = null);
