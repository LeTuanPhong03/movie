using MediatR;
using MovieReservation.Application.Common.Extensions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Movies.DTOs;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Movies.Create;

/// <summary>
/// Thêm phim mới: chuẩn hóa chuỗi (Trim, chuỗi rỗng → null cho trường không bắt buộc) rồi lưu.
/// Không chặn trùng tên vì có thể có phim cùng tên (bản làm lại, các phần khác nhau).
/// </summary>
public class CreateMovieCommandHandler(IAppDbContext db) : IRequestHandler<CreateMovieCommand, MovieDto>
{
    public async Task<MovieDto> Handle(CreateMovieCommand request, CancellationToken cancellationToken)
    {
        var movie = new Movie
        {
            Title = request.Title.Trim(),
            Description = request.Description.NullIfBlank(),
            DurationMinutes = request.DurationMinutes,
            Genre = request.Genre.Trim(),
            SuitableAge = request.SuitableAge,
            PosterUrl = request.PosterUrl.NullIfBlank(),
            IsActive = true
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync(cancellationToken);

        return MovieDto.FromEntity(movie);
    }
}
