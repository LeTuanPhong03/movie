using System.Security.Cryptography;
using System.Text;
using MovieReservation.Infrastructure.Payments;

namespace MovieReservation.UnitTests;

/// <summary>
/// Kiểm cách ký VNPay không cần DB: chuỗi ký sắp theo tên và bỏ tham số rỗng / vnp_SecureHash,
/// chữ ký = HMAC-SHA512 hex, và sửa bất kỳ tham số nào sau khi ký thì kiểm chữ ký thất bại.
/// </summary>
public class VnPaySignerTests
{
    private const string Secret = "SECRET";

    [Fact]
    public void BuildQuery_sap_theo_ten_url_encode_va_bo_tham_so_khong_ky()
    {
        var query = VnPaySigner.BuildQuery(new Dictionary<string, string>
        {
            ["vnp_TxnRef"] = "abc",
            ["vnp_Amount"] = "100",
            ["vnp_OrderInfo"] = "Thanh toan don",
            ["vnp_BankCode"] = "",          // rỗng → bỏ
            ["vnp_SecureHash"] = "xyz",     // chính là chữ ký → bỏ
            ["other"] = "1",                // không phải vnp_ → bỏ
        });

        Assert.Equal("vnp_Amount=100&vnp_OrderInfo=Thanh+toan+don&vnp_TxnRef=abc", query);
    }

    [Fact]
    public void Sign_la_HMAC_SHA512_dang_hex_thuong()
    {
        var expected = Convert.ToHexStringLower(
            HMACSHA512.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes("vnp_Amount=100")));

        Assert.Equal(expected, VnPaySigner.Sign("vnp_Amount=100", Secret));
        Assert.Equal(128, expected.Length);
    }

    [Fact]
    public void Verify_dung_khi_nguyen_ven_sai_khi_bi_sua_hoac_sai_secret()
    {
        var p = new Dictionary<string, string> { ["vnp_Amount"] = "100", ["vnp_ResponseCode"] = "24" };
        p[VnPaySigner.SecureHashKey] = VnPaySigner.Sign(VnPaySigner.BuildQuery(p), Secret);

        Assert.True(VnPaySigner.Verify(p, Secret));
        Assert.False(VnPaySigner.Verify(p, "SECRET_KHAC"));

        var biSua = new Dictionary<string, string>(p) { ["vnp_ResponseCode"] = "00" };
        Assert.False(VnPaySigner.Verify(biSua, Secret));

        var khongChuKy = new Dictionary<string, string>(p);
        khongChuKy.Remove(VnPaySigner.SecureHashKey);
        Assert.False(VnPaySigner.Verify(khongChuKy, Secret));
    }
}
