using MediatR;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.Create;
/// <summary>
/// đầu vào không có EndAt vì thời lượng phim được lấy từ Movie. EndAt = StartAt + Movie.Duration
/// </summary>
/// <param name="MovieId"></param>
/// <param name="StartAt"></param>
/// <param name="TheaterId"></param>
/// <param name="Price"></param>
public record CreateShowtimeCommand(int MovieId, DateTime StartAt, int TheaterId, decimal Price) : IRequest<ShowtimeDto>;
