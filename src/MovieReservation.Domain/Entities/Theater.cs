namespace MovieReservation.Domain.Entities;

/// <summary>
/// Lớp Theater đại diện cho một phòng chiếu phim, chứa thông tin về phòng và danh sách các ghế trong phòng.
/// hàm GenerateSeats để tạo ra danh sách ghế dựa trên số hàng và số ghế trên mỗi hàng
/// </summary>
public class Theater
{
    public const int MaxRows = 26; // Giới hạn số hàng tối đa là 26 (A-Z)
    public const int MaxSeatsPerRow = 50; // Giới hạn số ghế tối đa trên mỗi hàng là 50
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    private readonly List<Seat> _seats = new List<Seat>();
    public IReadOnlyCollection<Seat> Seats => _seats.AsReadOnly();

    public void GenerateSeats(int rows, int seatsPerRow)
    {
        if(_seats.Count > 0)
        {
            throw new InvalidOperationException("Danh sách ghế đã được tạo. Không thể tạo lại.");
        }
        if (rows < 1 || rows > MaxRows)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), $"Số hàng phải nằm trong khoảng từ 1 đến {MaxRows}.");
        }
        if (seatsPerRow < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(seatsPerRow), "Số ghế trên mỗi hàng phải lớn hơn 0.");
        }
        if(seatsPerRow > MaxSeatsPerRow)
        {
            throw new ArgumentOutOfRangeException(nameof(seatsPerRow), $"Số ghế trên mỗi hàng không được vượt quá {MaxSeatsPerRow}.");
        }
        for (int row = 0; row < rows; row++)
        {
            char rowLetter = (char)('A' + row);
            for (int seatNumber = 1; seatNumber <= seatsPerRow; seatNumber++)
            {
                _seats.Add(new Seat
                {
                    Row = rowLetter.ToString(),
                    Number = seatNumber
                });
            }
        }
    }
}