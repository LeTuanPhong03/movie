using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.DTOs;

/// <summary>
/// Kết quả đặt vé trả cho khách. Phần bắt buộc để làm bước tiếp: Id (để confirm/cancel), Status, TotalPrice, ExpiresAt (hạn thanh toán);
/// phần còn lại để hiển thị vé. Không trả độ tuổi/thời lượng vì đó là thông tin của phim, khách đã xem trước khi đặt.
/// </summary>
public record ReservationDto(
    int Id,
    ReservationStatus Status,
    decimal TotalPrice,
    DateTime ExpiresAt,
    int ShowtimeId,
    string MovieTitle,
    string TheaterName,
    DateTime StartAt,
    List<ReservedSeatDto> Seats);

public record ReservedSeatDto(string Row, int Number);
