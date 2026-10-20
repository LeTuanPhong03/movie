using System.Text.Json.Serialization;
using MediatR;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.Update;

/// <summary>
/// Yêu cầu sửa toàn bộ thông tin phim (PUT, chỉ Admin). Id lấy từ URL, không từ body
/// ([JsonIgnore] để client không gửi được Id khác với URL).
/// </summary>
public record UpdateMovieCommand(
    string Title,
    string? Description,
    int DurationMinutes,
    string Genre,
    int SuitableAge,
    string? PosterUrl) : IRequest<MovieDto>, IMovieInput
{
    [JsonIgnore]
    public int Id { get; init; }
}
