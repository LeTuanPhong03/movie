namespace MovieReservation.Application.Common.Interfaces;

/// <summary>
/// Cổng VNPay mà tầng Application cần: tạo link thanh toán có chữ ký, và kiểm chữ ký của dữ liệu VNPay gửi về
/// (IPN / return URL). Cách ký HMAC-SHA512 và cấu hình TmnCode/HashSecret nằm ở Infrastructure (VnPayGateway).
/// </summary>
public interface IVnPayGateway
{
    string CreatePaymentUrl(VnPayPaymentRequest request);

    /// <summary>true nếu vnp_SecureHash khớp với chữ ký tự tính lại từ các tham số còn lại.</summary>
    bool IsValidSignature(IReadOnlyDictionary<string, string> parameters);
}

/// <param name="Amount">Số tiền VND (gateway tự nhân 100 theo quy định VNPay).</param>
/// <param name="ExpiresAtUtc">Hết thời điểm này VNPay không cho thanh toán nữa.</param>
public record VnPayPaymentRequest(
    string TxnRef,
    decimal Amount,
    string OrderInfo,
    string ClientIp,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
