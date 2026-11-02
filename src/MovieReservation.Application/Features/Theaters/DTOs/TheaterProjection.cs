using System.Linq.Expressions;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Theaters.DTOs;

/// <summary>
/// Phép chiếu Theater → TheaterDto dùng chung cho xem một phòng và liệt kê phòng. Bảng Theaters không lưu
/// Rows/SeatsPerRow nên tính lại từ ghế thật trong SQL: số hàng = số giá trị Row khác nhau,
/// ghế mỗi hàng = Number lớn nhất (GenerateSeats đánh số từ 1, mọi hàng bằng nhau).
/// </summary>
public static class TheaterProjection
{
    public static readonly Expression<Func<Theater, TheaterDto>> ToDto = t => new TheaterDto(
        t.Id,
        t.Name,
        t.Seats.Select(s => s.Row).Distinct().Count(),
        t.Seats.Max(s => (int?)s.Number) ?? 0,
        t.Seats.Count);
}
