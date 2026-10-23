using MediatR;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.GetSeats;

/// <summary>
/// Yêu cầu lấy sơ đồ ghế (kèm trạng thái) của một suất chiếu cho khách xem.
/// </summary>
public record GetSeatsByShowtimeQuery(int ShowtimeId) : IRequest<SeatMapDto>;
