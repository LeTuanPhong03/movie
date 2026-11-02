using MediatR;
using MovieReservation.Application.Features.Reservations.DTOs;

namespace MovieReservation.Application.Features.Reservations.GetById;

/// <summary>
/// Xem chi tiết một đơn. Chủ đơn xem được đơn của mình, Admin xem được mọi đơn (UserId, IsAdmin đọc từ token).
/// </summary>
public record GetReservationByIdQuery(int ReservationId, int UserId, bool IsAdmin) : IRequest<ReservationDto>;
