using MediatR;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Payments.VnPayCallback;

/// <summary>
/// Xử lý kết quả thanh toán VNPay gửi về: nguyên bộ tham số vnp_* lấy từ query string của IPN
/// (server VNPay gọi server mình) hoặc của return URL (trình duyệt khách quay về). Cả hai cùng một lệnh,
/// an toàn vì lệnh idempotent: ai đến trước xử lý, ai đến sau nhận "02 đã xử lý".
/// </summary>
public record ProcessVnPayCallbackCommand(IReadOnlyDictionary<string, string> Parameters) : IRequest<VnPayCallbackResult>;

/// <param name="RspCode">Mã trả lời cho VNPay theo tài liệu IPN: 00 ok, 01 không thấy đơn, 02 đã xử lý, 04 sai tiền, 97 sai chữ ký.</param>
public record VnPayCallbackResult(string RspCode, string Message, int? ReservationId = null, PaymentStatus? PaymentStatus = null);
