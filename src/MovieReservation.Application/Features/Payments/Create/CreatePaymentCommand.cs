using MediatR;

namespace MovieReservation.Application.Features.Payments.Create;

/// <summary>
/// Lệnh tạo link thanh toán VNPay cho một đơn Pending của chính người gọi.
/// UserId và ClientIp (VNPay bắt buộc gửi IP người mua) do controller lấy từ token / kết nối.
/// </summary>
public record CreatePaymentCommand(int ReservationId, int UserId, string ClientIp) : IRequest<PaymentLinkDto>;

/// <param name="ExpiresAt">Hạn giữ ghế của đơn = hạn VNPay còn cho thanh toán.</param>
public record PaymentLinkDto(string PaymentUrl, DateTime ExpiresAt);
