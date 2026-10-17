namespace MovieReservation.Domain.Entities;
public class Seat
{
    public int Id { get; set; }
    public int TheaterId { get; set; }
    public string Row { get; set; } = string.Empty;
    public int Number { get; set; }
}