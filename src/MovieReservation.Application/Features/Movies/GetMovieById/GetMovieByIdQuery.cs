using MediatR;
using MovieReservation.Application.Features.Movies.DTOs;

namespace MovieReservation.Application.Features.Movies.GetMovieById;

/// <summary>
/// Lấy chi tiết một phim đang chiếu theo Id. Phim đã ẩn coi như không tồn tại với khách (404).
/// </summary>
public record GetMovieByIdQuery(int Id) : IRequest<MovieDto>;
