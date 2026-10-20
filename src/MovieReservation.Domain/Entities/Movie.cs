namespace MovieReservation.Domain.Entities;

/// <summary>
/// Phim chiếu tại rạp. Không xóa thật: "xóa" = IsActive = false (soft delete) để giữ lịch sử suất chiếu và vé đã bán.
/// </summary>
public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public string Genre { get; set; } = string.Empty;
    public int SuitableAge { get; set; }
    public string? PosterUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
