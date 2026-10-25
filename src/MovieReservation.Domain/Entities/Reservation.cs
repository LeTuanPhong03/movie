namespace MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

/// <summary>
/// Một đơn đặt vé của khách cho một suất chiếu. Mới tạo là Pending và giữ ghế HoldMinutes phút;
/// quá ExpiresAt mà chưa thanh toán thì ghế được coi như trống (hết hạn lười).
/// MaxSeats giới hạn số ghế mỗi đơn để chống giữ chỗ ảo (một người khóa cả rạp miễn phí).
/// </summary>
public class Reservation
{
    public const int MaxSeats = 8;
    public const int HoldMinutes = 10;

    public int Id { get; set; }
    public int UserId { get; set; }
    public int ShowtimeId { get; set; }
    public ReservationStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
}
