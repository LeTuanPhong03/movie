using MediatR;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.Create;

/// <summary>
/// Yêu cầu thêm phim mới (chỉ Admin). Phim mới luôn ở trạng thái đang chiếu (IsActive = true).
/// </summary>
public record CreateMovieCommand(
    string Title,
    string? Description,
    int DurationMinutes,
    string Genre,
    int SuitableAge,
    string? PosterUrl) : IRequest<MovieDto>, IMovieInput;
