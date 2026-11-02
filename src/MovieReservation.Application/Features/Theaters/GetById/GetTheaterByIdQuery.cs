using MediatR;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Application.Features.Theaters.GetById;

/// <summary>
/// Xem một phòng chiếu theo Id. Có endpoint này thì header Location của POST /api/theaters mới trỏ tới chỗ có thật.
/// </summary>
public record GetTheaterByIdQuery(int Id) : IRequest<TheaterDto>;
