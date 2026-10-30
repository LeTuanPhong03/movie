using MediatR;

namespace MovieReservation.Application.Features.Reservations.Cancel;

/// <summary>
/// Lệnh huỷ một đơn Pending hoặc Confirmed của suất chưa chiếu. UserId và IsAdmin do controller
/// đọc từ token (Application không biết HTTP): chủ đơn huỷ được đơn của mình, Admin huỷ được mọi đơn.
/// </summary>
public record CancelReservationCommand(int ReservationId, int UserId, bool IsAdmin) : IRequest;
