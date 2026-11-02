using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MovieReservation.Infrastructure.Payments;

/// <summary>
/// Cách VNPay (bản 2.1.0) ký dữ liệu: sắp các tham số vnp_* theo tên (so sánh ordinal), bỏ giá trị rỗng,
/// URL-encode từng key/value, nối "k=v&amp;k=v" → HMAC-SHA512 với HashSecret → chuỗi hex.
/// Chuỗi "k=v&amp;..." đó cũng chính là query string của link thanh toán. Tách riêng để unit test được.
/// </summary>
public static class VnPaySigner
{
    public const string SecureHashKey = "vnp_SecureHash";
    public const string SecureHashTypeKey = "vnp_SecureHashType";

    public static string BuildQuery(IEnumerable<KeyValuePair<string, string>> parameters) =>
        string.Join("&", parameters
            .Where(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal)
                     && p.Key != SecureHashKey && p.Key != SecureHashTypeKey
                     && !string.IsNullOrEmpty(p.Value))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

    public static string Sign(string data, string secret)
    {
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(data));
        return Convert.ToHexStringLower(hash);
    }

    public static bool Verify(IReadOnlyDictionary<string, string> parameters, string secret)
    {
        if (!parameters.TryGetValue(SecureHashKey, out var received) || string.IsNullOrEmpty(received))
            return false;
        var expected = Sign(BuildQuery(parameters), secret);
        // So sánh thời gian cố định: không để lộ "đúng được bao nhiêu ký tự đầu" qua thời gian phản hồi
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(received.ToLowerInvariant()));
    }
}
