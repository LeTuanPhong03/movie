using MediatR;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.GetByDate;
/// <summary>
/// Yêu cầu lấy danh sách suất chiếu của một ngày (theo giờ Việt Nam) cho khách xem.
/// </summary>
public record GetShowtimesByDateQuery(DateOnly Date) : IRequest<List<ShowtimeDto>>;