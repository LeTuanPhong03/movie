namespace MovieReservation.Application.Features.Movies;

/// <summary>
/// Các trường phim mà admin nhập, chung cho CreateMovieCommand và UpdateMovieCommand.
/// Nhờ interface này, hai command dùng chung một bộ luật kiểm tra (MovieInputValidator).
/// </summary>
public interface IMovieInput
{
    string Title { get; }
    string? Description { get; }
    int DurationMinutes { get; }
    string Genre { get; }
    int SuitableAge { get; }
    string? PosterUrl { get; }
}
