using MediatR;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Showtimes.DTOs;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Showtimes.Create;
/// <summary>
/// CreateShowtimeCommandHandler xử lý lệnh tạo suất chiếu mới. Nó kiểm tra sự tồn tại và trạng thái của phim, phòng chiếu, và kiểm tra xung đột lịch chiếu trước khi tạo suất chiếu mới và lưu vào cơ sở dữ liệu.
/// </summary>
/// <param name="db"></param>
public class CreateShowtimeCommandHandler(IAppDbContext db) : IRequestHandler<CreateShowtimeCommand, ShowtimeDto>
{
    public async Task<ShowtimeDto> Handle(CreateShowtimeCommand request, CancellationToken cancellationToken)
    {
        // kiem tra xem phim co ton tai hay khong
        var movie = await db.Movies.FirstOrDefaultAsync(x => x.Id == request.MovieId, cancellationToken);
        if(movie == null)
        {
            throw new NotFoundException($"Movie with Id {request.MovieId} not found.");
        }
        if(!movie.IsActive)
        {
            throw new ConflictException($"Movie with Id {request.MovieId} is not active.");
        }
        
        // tim phong chieu co ton tai hay khong
        var theater = await db.Theaters.Include(x => x.Seats).FirstOrDefaultAsync(x => x.Id == request.TheaterId, cancellationToken);
        if(theater == null)
        {
            throw new NotFoundException($"Theater with Id {request.TheaterId} not found.");
        }
        var endAt = request.StartAt.AddMinutes(movie.DurationMinutes) + TimeSpan.FromMinutes(Showtime.CleanupMinutes);
        // kiểm tra trùng khi suất mới bắt đầu trước lúc suất cũ kết thúc và suất mới kết thúc sau lúc suất cũ bắt đầu (chỉ chạm nhau thì không trùng)
        var kiemTraTrung = await db.Showtimes.FirstOrDefaultAsync(x => x.TheaterId == request.TheaterId && 
            request.StartAt < x.EndAt && endAt > x.StartAt, cancellationToken);
        if(kiemTraTrung != null)
        {
            throw new ConflictException($"Trùng lịch với showtime có Id {kiemTraTrung.Id}.");
        }
        // tao showtime moi
        var showtime = new Showtime
        {
            MovieId = request.MovieId,
            StartAt = request.StartAt,
            EndAt = endAt,
            TheaterId = request.TheaterId,
            Price = request.Price
        };
        showtime.GenerateShowtimeSeats(theater.Seats);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync(cancellationToken);
        var showtimeDto = new ShowtimeDto
        (
            Id: showtime.Id,
            TheaterName: theater.Name,
            StartAt: showtime.StartAt,
            EndAt: showtime.EndAt,
            Title: movie.Title,
            Price: showtime.Price
        );
        return showtimeDto;
    }
}
