using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Movies.DTOs;

/// <summary>
/// Thông tin phim trả cho client. FromEntity gom việc chuyển entity → DTO về một chỗ để các handler dùng chung.
/// </summary>
public record MovieDto(
    int Id,
    string Title,
    string? Description,
    int DurationMinutes,
    string Genre,
    int SuitableAge,
    string? PosterUrl,
    bool IsActive)
{
    public static MovieDto FromEntity(Movie movie) => new(
        Id: movie.Id,
        Title: movie.Title,
        Description: movie.Description,
        DurationMinutes: movie.DurationMinutes,
        Genre: movie.Genre,
        SuitableAge: movie.SuitableAge,
        PosterUrl: movie.PosterUrl,
        IsActive: movie.IsActive);
}
