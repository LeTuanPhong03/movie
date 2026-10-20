using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Extensions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.Update;

/// <summary>
/// Sửa phim. Không cho đổi thời lượng khi phim còn suất chiếu chưa kết thúc (EndAt > now), vì EndAt đã lưu
/// sẽ sai và có thể chồng giờ trong phòng (docs/decisions.md mục 5). Admin sửa được cả phim đã ẩn.
/// </summary>
public class UpdateMovieCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<UpdateMovieCommand, MovieDto>
{
    public async Task<MovieDto> Handle(UpdateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phim.");

        if (request.DurationMinutes != movie.DurationMinutes)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            // EndAt (không phải StartAt) để tính cả suất đang chiếu dở
            var hasUnfinishedShowtime = await db.Showtimes
                .AnyAsync(s => s.MovieId == movie.Id && s.EndAt > now, cancellationToken);
            if (hasUnfinishedShowtime)
            {
                throw new ConflictException("Phim còn suất chiếu chưa kết thúc, không thể đổi thời lượng.");
            }
        }

        // Entity lấy từ DB đang được DbContext theo dõi → chỉ cần gán rồi SaveChanges, không cần gọi Update(...)
        movie.Title = request.Title.Trim();
        movie.Description = request.Description.NullIfBlank();
        movie.DurationMinutes = request.DurationMinutes;
        movie.Genre = request.Genre.Trim();
        movie.SuitableAge = request.SuitableAge;
        movie.PosterUrl = request.PosterUrl.NullIfBlank();
        await db.SaveChangesAsync(cancellationToken);

        return MovieDto.FromEntity(movie);
    }
}
