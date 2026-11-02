namespace MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

/// <summary>
/// Một đơn đặt vé của khách cho một suất chiếu. Mới tạo là Pending và giữ ghế HoldMinutes phút;
/// quá ExpiresAt mà chưa thanh toán thì ghế được coi như trống (hết hạn lười).
/// MaxSeats giới hạn số ghế mỗi đơn để chống giữ chỗ ảo (một người khóa cả rạp miễn phí).
/// Seats là bản chụp các ghế đã đặt (ReservationSeat), giữ nguyên kể cả khi đơn bị huỷ/hết hạn.
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
    public Showtime Showtime { get; set; } = null!;

    private readonly List<ReservationSeat> _seats = new();
    public IReadOnlyCollection<ReservationSeat> Seats => _seats.AsReadOnly();

    public void AddSeat(int seatId)
    {
        if (_seats.Any(s => s.SeatId == seatId))
            throw new InvalidOperationException($"Ghế {seatId} đã có trong đơn.");
        _seats.Add(new ReservationSeat { SeatId = seatId });
    }
}
