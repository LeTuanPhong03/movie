namespace MovieReservation.Application.Features.Showtimes.DTOs;

/// <summary>
/// Dữ liệu suất chiếu trả về cho client: kèm tên phim và tên phòng để hiển thị luôn, không cần gọi thêm API.
/// </summary>
public record ShowtimeDto(int Id, string Title, DateTime StartAt, DateTime EndAt, string TheaterName, decimal Price);
