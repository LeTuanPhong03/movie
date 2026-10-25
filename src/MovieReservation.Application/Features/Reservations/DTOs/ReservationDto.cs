using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.DTOs;

/// <summary>
/// Đơn đặt trả cho khách — CHƯA phải vé (vé mỗi ghế một cái, chỉ có sau khi thanh toán/Confirmed).
/// Trường bắt buộc để làm bước tiếp: Id (confirm/cancel), Status, TotalPrice, ExpiresAt (hạn thanh toán).
/// Phần còn lại để khách xem lại đơn trước khi trả tiền. ShowtimeId để link về suất khi tái dùng ở xem chi tiết/lịch sử.
/// MovieEndAt = StartAt + thời lượng phim, KHÔNG dùng Showtime.EndAt (đã cộng thời gian dọn phòng).
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
    DateTime MovieEndAt,
    List<ReservedSeatDto> Seats);

public record ReservedSeatDto(string Row, int Number);
