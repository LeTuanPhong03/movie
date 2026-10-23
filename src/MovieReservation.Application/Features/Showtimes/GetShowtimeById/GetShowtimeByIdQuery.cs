using MediatR;
using MovieReservation.Application.Features.Showtimes.DTOs;

namespace MovieReservation.Application.Features.Showtimes.GetShowtimeById;

/// <summary>
/// Yêu cầu lấy chi tiết một suất chiếu theo Id (dùng cho trang chi tiết và header Location sau khi tạo suất).
/// </summary>
public record GetShowtimeByIdQuery(int Id) : IRequest<ShowtimeDto>;
