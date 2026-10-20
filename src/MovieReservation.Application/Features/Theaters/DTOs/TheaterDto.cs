namespace MovieReservation.Application.Features.Theaters.DTOs;

/// <summary>
/// Thông tin phòng chiếu trả cho client sau khi tạo: đủ để admin kiểm tra nhanh sơ đồ đã đúng chưa.
/// Danh sách ghế chi tiết lấy ở endpoint sơ đồ ghế, không trả ở đây.
/// </summary>
public record TheaterDto(int Id, string Name, int Rows, int SeatsPerRow, int TotalSeats);
