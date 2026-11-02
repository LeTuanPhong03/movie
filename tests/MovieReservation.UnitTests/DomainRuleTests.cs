using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.UnitTests;

/// <summary>
/// Luật thuần trong Domain, không cần DB: ghế khi nào được coi là trống (hết hạn lười),
/// đơn không nhận cùng một ghế 2 lần, sinh ghế của phòng và của suất chỉ được 1 lần.
/// </summary>
public class DomainRuleTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(SeatStatus.Available, null, true)]
    [InlineData(SeatStatus.Held, 1, false)]      // còn 1 phút mới hết giữ
    [InlineData(SeatStatus.Held, 0, true)]       // đúng lúc hết hạn → trống
    [InlineData(SeatStatus.Held, -5, true)]      // quá hạn 5 phút, job chưa dọn → vẫn coi là trống
    [InlineData(SeatStatus.Booked, null, false)] // đã bán thì không bao giờ trống
    public void IsAvailableAt_tinh_ca_ghe_Held_da_het_han(SeatStatus status, int? heldMinutesFromNow, bool expected)
    {
        var seat = new ShowtimeSeat
        {
            Status = status,
            HeldUntil = heldMinutesFromNow is int m ? Now.AddMinutes(m) : null
        };

        Assert.Equal(expected, seat.IsAvailableAt(Now));
    }

    [Fact]
    public void Reservation_AddSeat_trung_ghe_thi_nem_loi()
    {
        var reservation = new Reservation();
        reservation.AddSeat(1);
        reservation.AddSeat(2);

        Assert.Throws<InvalidOperationException>(() => reservation.AddSeat(1));
        Assert.Equal(2, reservation.Seats.Count);
    }

    [Fact]
    public void Theater_GenerateSeats_dat_ten_A1_den_hang_cuoi_va_chi_1_lan()
    {
        var theater = new Theater();
        theater.GenerateSeats(rows: 3, seatsPerRow: 4);

        Assert.Equal(12, theater.Seats.Count);
        Assert.Equal("C4", theater.Seats.Last().Row + theater.Seats.Last().Number);
        Assert.Throws<InvalidOperationException>(() => theater.GenerateSeats(1, 1));
    }

    [Fact]
    public void Showtime_GenerateShowtimeSeats_moi_ghe_Available_va_chi_1_lan()
    {
        var theater = new Theater();
        theater.GenerateSeats(rows: 1, seatsPerRow: 5);
        var showtime = new Showtime();

        showtime.GenerateShowtimeSeats(theater.Seats);

        Assert.Equal(5, showtime.ShowtimeSeats.Count);
        Assert.All(showtime.ShowtimeSeats, s => Assert.Equal(SeatStatus.Available, s.Status));
        Assert.Throws<InvalidOperationException>(() => showtime.GenerateShowtimeSeats(theater.Seats));
    }
}
