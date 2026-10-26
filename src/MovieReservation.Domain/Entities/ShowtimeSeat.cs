namespace MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

/// <summary>
/// Trạng thái một ghế trong một suất chiếu (Available → Held → Booked).
/// Navigation Reservation để gán đơn mới tạo khi chưa có Id: EF tự INSERT đơn trước rồi điền ReservationId, trong cùng một SaveChanges.
/// </summary>
public class ShowtimeSeat
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public SeatStatus Status { get; set; }
    public int? ReservationId { get; set; }
    public DateTime? HeldUntil { get; set; }
    public uint RowVersion { get; set; }
    public Seat Seat { get; set; } = null!;
    public Reservation? Reservation { get; set; }

    // Hết hạn lười: ghế Held đã quá HeldUntil được coi như trống, không cần job dọn
    public bool IsAvailableAt(DateTime now) =>
        Status == SeatStatus.Available || (Status == SeatStatus.Held && HeldUntil <= now);
}
