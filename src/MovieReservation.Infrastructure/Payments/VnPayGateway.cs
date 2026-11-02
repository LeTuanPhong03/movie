using System.Globalization;
using Microsoft.Extensions.Configuration;
using MovieReservation.Application.Common.Interfaces;

namespace MovieReservation.Infrastructure.Payments;

/// <summary>
/// Cài đặt IVnPayGateway cho VNPay sandbox. TmnCode/HashSecret là bí mật → user-secrets hoặc biến môi trường
/// (VnPay__TmnCode, VnPay__HashSecret), không bao giờ nằm trong appsettings. Thiếu cấu hình thì app vẫn chạy,
/// chỉ khi gọi tới thanh toán mới lỗi (để deploy được trước khi có tài khoản VNPay).
/// Giờ gửi cho VNPay (vnp_CreateDate/vnp_ExpireDate) là giờ Việt Nam dạng yyyyMMddHHmmss.
/// </summary>
public class VnPayGateway(IConfiguration configuration) : IVnPayGateway
{
    private static readonly TimeZoneInfo Vietnam = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public string CreatePaymentUrl(VnPayPaymentRequest request)
    {
        var parameters = new Dictionary<string, string>
        {
            ["vnp_Version"] = "2.1.0",
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = Required("VnPay:TmnCode"),
            // VNPay tính tiền theo đơn vị nhỏ nhất: 90.000đ gửi là 9000000
            ["vnp_Amount"] = ((long)(request.Amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_TxnRef"] = request.TxnRef,
            ["vnp_OrderInfo"] = request.OrderInfo,
            ["vnp_OrderType"] = "other",
            ["vnp_Locale"] = "vn",
            ["vnp_ReturnUrl"] = Required("VnPay:ReturnUrl"),
            ["vnp_IpAddr"] = request.ClientIp,
            ["vnp_CreateDate"] = ToVietnamString(request.CreatedAtUtc),
            ["vnp_ExpireDate"] = ToVietnamString(request.ExpiresAtUtc),
        };
        var query = VnPaySigner.BuildQuery(parameters);
        var signature = VnPaySigner.Sign(query, Required("VnPay:HashSecret"));
        return $"{Required("VnPay:PaymentUrl")}?{query}&{VnPaySigner.SecureHashKey}={signature}";
    }

    public bool IsValidSignature(IReadOnlyDictionary<string, string> parameters) =>
        VnPaySigner.Verify(parameters, Required("VnPay:HashSecret"));

    private string Required(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Chưa cấu hình {key} (xem README mục VNPay).");

    private static string ToVietnamString(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(utc, Vietnam).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
}
