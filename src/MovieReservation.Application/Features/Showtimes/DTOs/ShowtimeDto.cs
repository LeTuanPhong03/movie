namespace MovieReservation.Application.Features.Showtimes.DTOs;

/// <summary>
/// Dữ liệu suất chiếu trả về cho client: kèm tên phim và tên phòng để hiển thị luôn, không cần gọi thêm API.
/// MovieId để client nối suất ↔ phim (tên phim KHÔNG unique — hai phim có thể trùng tên, ví dụ bản làm lại).
/// MovieEndAt = giờ phim hết (StartAt + thời lượng), KHÔNG phải Showtime.EndAt (đã cộng 15' dọn phòng, chỉ dùng nội bộ để chống trùng giờ).
/// </summary>
public record ShowtimeDto(int Id, int MovieId, string Title, DateTime StartAt, DateTime MovieEndAt, string TheaterName, decimal Price);
