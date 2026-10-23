namespace MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;
public class ShowtimeSeat
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public SeatStatus Status { get; set; }
    public int? ReservationId { get; set; }
    public DateTime? HeldUntil { get; set; }
    public int RowVersion { get; set; }
    public Seat Seat { get; set; } = null!;
}