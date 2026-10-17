namespace MovieReservation.Domain.Entities;
public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string Genre { get; set; } = string.Empty;
    public int SuitableAge { get; set; }
    public string? PosterUrl { get; set; }
}