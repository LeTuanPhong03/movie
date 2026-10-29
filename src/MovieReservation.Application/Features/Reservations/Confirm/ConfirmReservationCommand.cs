using MediatR;

namespace MovieReservation.Application.Features.Reservations.Confirm;

/// <summary>
/// Lệnh xác nhận (thanh toán) một đơn đang Pending. UserId do controller lấy từ token JWT,
/// dùng để chỉ chủ đơn mới confirm được đơn của mình.
/// </summary>
public record ConfirmReservationCommand(int ReservationId, int UserId) : IRequest;
