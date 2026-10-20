using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.GetMovies;

/// <summary>
/// Danh sách phim đang chiếu (IsActive) cho khách, sắp theo tên. Đếm tổng trước, rồi lấy đúng một trang bằng Skip/Take.
/// </summary>
public class GetMoviesQueryHandler(IAppDbContext db) : IRequestHandler<GetMoviesQuery, PagedResult<MovieDto>>
{
    public async Task<PagedResult<MovieDto>> Handle(GetMoviesQuery request, CancellationToken cancellationToken)
    {
        // Chỉ đọc, không sửa → AsNoTracking cho nhẹ
        var query = db.Movies.AsNoTracking().Where(m => m.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        // Bắt buộc OrderBy trước Skip/Take, không thì thứ tự không ổn định → trang trùng/sót phim.
        // ThenBy(Id) vì có thể có 2 phim cùng tên.
        var movies = await query
            .OrderBy(m => m.Title)
            .ThenBy(m => m.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = movies.Select(MovieDto.FromEntity).ToList();
        return new PagedResult<MovieDto>(items, request.Page, request.PageSize, totalCount);
    }
}
