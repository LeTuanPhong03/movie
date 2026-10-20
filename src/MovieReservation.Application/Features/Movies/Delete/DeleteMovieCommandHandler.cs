using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;

namespace MovieReservation.Application.Features.Movies.Delete;

/// <summary>
/// Xóa phim. Không tồn tại → 404.
/// Mặc định (soft): IsActive = false; phim đã ẩn rồi → coi như xong (idempotent, giống Logout); suất chiếu và vé cũ giữ nguyên.
/// HardDelete = true: xóa hẳn dòng trong DB, chỉ khi phim chưa từng có suất chiếu (kể cả suất đã chiếu xong) → nếu có thì 409.
/// </summary>
public class DeleteMovieCommandHandler(IAppDbContext db) : IRequestHandler<DeleteMovieCommand>
{
    public async Task Handle(DeleteMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = await db.Movies.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy phim.");
        if (!request.HardDelete)
        {
            if (!movie.IsActive)
            {
                return; // idempotent, coi như xong
            }
            movie.IsActive = false;
        }
        else
        {
            var hasShowtimes = await db.Showtimes.AnyAsync(s => s.MovieId == request.Id, cancellationToken);
            if (hasShowtimes) 
            {
                throw new ConflictException("Phim đã có suất chiếu, chỉ có thể ẩn.");
            }
            db.Movies.Remove(movie);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
