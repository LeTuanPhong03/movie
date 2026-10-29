using MediatR;

namespace MovieReservation.Application.Features.Reservations.Expire;

/// <summary>
/// Lệnh quét các đơn Pending đã quá ExpiresAt: chuyển sang Expired và nhả ghế về Available.
/// Do ReservationExpiryJob gọi định kỳ; trả về số đơn đã xử lý để job ghi log.
/// </summary>
public record ExpireReservationsCommand : IRequest<int>;
