using MediatR;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.GetMovies;

/// <summary>
/// Lấy danh sách phim đang chiếu, có phân trang. Không gửi tham số thì mặc định trang 1, 10 phim/trang.
/// </summary>
public record GetMoviesQuery(int Page = 1, int PageSize = 10) : IRequest<PagedResult<MovieDto>>
{
    public const int MaxPageSize = 50;
}
