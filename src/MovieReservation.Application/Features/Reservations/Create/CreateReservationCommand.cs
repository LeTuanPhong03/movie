using MediatR;
using MovieReservation.Application.Features.Reservations.DTOs;

namespace MovieReservation.Application.Features.Reservations.Create;

/// <summary>
/// Lệnh đặt vé: giữ các ghế (Id của ShowtimeSeat, lấy từ sơ đồ ghế) của một suất cho người dùng.
/// UserId do controller lấy từ token JWT, không nhận từ body (tránh đặt vé dưới tên người khác).
/// </summary>
public record CreateReservationCommand(int UserId, int ShowtimeId, List<int> SeatIds) : IRequest<ReservationDto>;
