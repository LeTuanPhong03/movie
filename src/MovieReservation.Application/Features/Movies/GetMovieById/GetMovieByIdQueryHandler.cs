using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.GetMovieById;

/// <summary>
/// Chi tiết một phim cho khách: chỉ trả phim đang chiếu; không thấy hoặc đã ẩn → NotFoundException (404).
/// </summary>
public class GetMovieByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetMovieByIdQuery, MovieDto>
{
    public async Task<MovieDto> Handle(GetMovieByIdQuery request, CancellationToken cancellationToken)
    {
        var movie = await db.Movies.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.IsActive, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phim.");

        return MovieDto.FromEntity(movie);
    }
}
