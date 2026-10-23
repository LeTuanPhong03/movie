using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.GetShowtimeById;

/// <summary>
/// Chi tiết một suất chiếu kèm tên phim và tên phòng. Không thấy, hoặc phim đã ẩn (giống GetMovieById) → NotFoundException (404).
/// Suất đã chiếu vẫn trả về để xem lại lịch sử vé.
/// </summary>
public class GetShowtimeByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetShowtimeByIdQuery, ShowtimeDto>
{
    public async Task<ShowtimeDto> Handle(GetShowtimeByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Showtimes
            .AsNoTracking()
            .Where(s => s.Id == request.Id && s.Movie.IsActive)
            .Select(s => new ShowtimeDto(
                Id: s.Id,
                Title: s.Movie.Title,
                StartAt: s.StartAt,
                EndAt: s.EndAt,
                TheaterName: s.Theater.Name,
                Price: s.Price))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy suất chiếu có Id {request.Id}.");
    }
}
