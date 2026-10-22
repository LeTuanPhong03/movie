using MovieReservation.Domain.Enums;

namespace MovieReservation.Domain.Entities;
/// <summary>
/// class này đại diện 1 suất chiếu phim.
/// có 1 danh sách trạng thái ghế rạp chiếu (ShowtimeSeat) để đặt vé.
/// 1 hàm GenerateShowtimeSeats() để tạo danh sách ghế rạp chiếu từ danh sách ghế cứng trong rạp chiếu (Seat).
/// </summary>
public class Showtime
{
    public const int CleanupMinutes = 15;
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int TheaterId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public decimal Price { get; set; }

    private readonly List<ShowtimeSeat> _showtimeSeats = new();
    public IReadOnlyCollection<ShowtimeSeat> ShowtimeSeats => _showtimeSeats.AsReadOnly();

    public void GenerateShowtimeSeats(IReadOnlyCollection<Seat> theaterSeats)
    {
        // Kiểm tra danh sách ghế rạp chiếu có hợp lệ hay không (danh sách ghế cứng trong rạp chiếu)
        if(theaterSeats == null || !theaterSeats.Any())
        {
            throw new InvalidOperationException("Danh sách ghế rạp chiếu không hợp lệ. Vui lòng kiểm tra lại danh sách ghế.");
        }
        // Đã có ghế rạp chiếu, không cần tạo lại (danh sách ghế để đặt vé)
        if (_showtimeSeats.Any())
        {
            throw new InvalidOperationException("Danh sách ghế rạp chiếu đã tồn tại. Không thể tạo lại danh sách ghế.");
        }
        foreach (var seat in theaterSeats)
        {
            var showtimeSeat = new ShowtimeSeat
            {
                SeatId = seat.Id,
                Status = SeatStatus.Available,
            };
            _showtimeSeats.Add(showtimeSeat);
        }
    }
}