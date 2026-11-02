using MediatR;

namespace MovieReservation.Application.Features.Reports.Capacity;

/// <summary>
/// Báo cáo công suất cho Admin (đề bài: capacity): mỗi suất chiếu bắt đầu trong khoảng ngày [From, To]
/// (giờ Việt Nam) có bao nhiêu ghế đã bán, đang giữ, còn trống và tỉ lệ lấp đầy.
/// </summary>
public record GetCapacityReportQuery(DateOnly From, DateOnly To) : IRequest<CapacityReportDto>
{
    public const int MaxDays = 31;
}

public record CapacityReportDto(
    DateOnly From,
    DateOnly To,
    int TotalSeats,
    int BookedSeats,
    double OccupancyPercent,
    List<CapacityRowDto> Rows);

/// <param name="HeldSeats">Ghế đang giữ còn hạn (Held quá HeldUntil được tính là trống, giống IsAvailableAt).</param>
/// <param name="OccupancyPercent">Booked / Total × 100, làm tròn 1 chữ số; chỉ tính ghế đã bán, không tính ghế đang giữ.</param>
public record CapacityRowDto(
    int ShowtimeId,
    int MovieId,
    string MovieTitle,
    string TheaterName,
    DateTime StartAt,
    int TotalSeats,
    int BookedSeats,
    int HeldSeats,
    int AvailableSeats,
    double OccupancyPercent);
