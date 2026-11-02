using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Payments.Create;
using MovieReservation.Application.Features.Payments.VnPayCallback;
using MovieReservation.Domain.Enums;
using MovieReservation.Infrastructure.Payments;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Thanh toán VNPay với VnPayGateway thật (secret giả): link có chữ ký đúng; IPN sai chữ ký / sai tiền bị từ chối
/// mà DB không đổi; IPN thành công → đơn Confirmed + ghế Booked; IPN lặp lại hoặc 10 IPN song song chỉ xác nhận 1 lần;
/// thanh toán thất bại giữ đơn Pending; trả tiền khi đơn đã hết hạn → Payment RefundRequired.
/// </summary>
public class VnPayPaymentTests : PostgresTestBase
{
    private const string Secret = "TEST_HASH_SECRET";
    private const string PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    private static readonly VnPayGateway Gateway = new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VnPay:TmnCode"] = "TESTCODE",
            ["VnPay:HashSecret"] = Secret,
            ["VnPay:PaymentUrl"] = PaymentUrl,
            ["VnPay:ReturnUrl"] = "http://localhost/payment-result.html",
        })
        .Build());

    private async Task<PaymentLinkDto> TaoLinkAsync(TimeProvider clock, int reservationId, int userId)
    {
        await using var db = NewDb();
        return await new CreatePaymentCommandHandler(db, Gateway, clock)
            .Handle(new CreatePaymentCommand(reservationId, userId, "127.0.0.1"), CancellationToken.None);
    }

    private async Task<VnPayCallbackResult> IpnAsync(TimeProvider clock, Dictionary<string, string> parameters)
    {
        await using var db = NewDb();
        return await new ProcessVnPayCallbackCommandHandler(db, Gateway, clock, NullLogger<ProcessVnPayCallbackCommandHandler>.Instance)
            .Handle(new ProcessVnPayCallbackCommand(parameters), CancellationToken.None);
    }

    // Giả lập dữ liệu VNPay gửi về, ký bằng đúng secret như VNPay thật
    private static Dictionary<string, string> KetQuaVnPay(string txnRef, decimal amount, string responseCode = "00")
    {
        var p = new Dictionary<string, string>
        {
            ["vnp_TmnCode"] = "TESTCODE",
            ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
            ["vnp_BankCode"] = "NCB",
            ["vnp_OrderInfo"] = "Thanh toan don ve",
            ["vnp_ResponseCode"] = responseCode,
            ["vnp_TransactionStatus"] = responseCode,
            ["vnp_TransactionNo"] = "14000001",
            ["vnp_TxnRef"] = txnRef,
        };
        p[VnPaySigner.SecureHashKey] = VnPaySigner.Sign(VnPaySigner.BuildQuery(p), Secret);
        return p;
    }

    private async Task<(int DonId, string TxnRef, Seeded S)> DatVaTaoLinkAsync(TimeProvider clock)
    {
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]]);
        await TaoLinkAsync(clock, donId, s.UserA);
        await using var db = NewDb();
        var txnRef = await db.Payments.Where(p => p.ReservationId == donId).Select(p => p.TxnRef).SingleAsync();
        return (donId, txnRef, s);
    }

    [Fact]
    public async Task Link_thanh_toan_co_chu_ky_dung_va_so_tien_nhan_100()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]]);

        var link = await TaoLinkAsync(clock, donId, s.UserA);

        Assert.StartsWith(PaymentUrl + "?", link.PaymentUrl);
        var query = QueryHelpers.ParseQuery(new Uri(link.PaymentUrl).Query).ToDictionary(q => q.Key, q => q.Value.ToString());
        Assert.Equal("18000000", query["vnp_Amount"]); // 2 ghế x 90.000đ x 100
        Assert.True(VnPaySigner.Verify(query, Secret));
        // 17:10 UTC = 00:10 giờ VN ngày 2/1: hạn VNPay đúng bằng hạn giữ ghế, đúng múi giờ
        Assert.Equal("20300102001000", query["vnp_ExpireDate"]);
    }

    [Fact]
    public async Task Tao_link_cho_don_nguoi_khac_404_don_da_confirm_409()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);

        await Assert.ThrowsAsync<NotFoundException>(() => TaoLinkAsync(clock, donId, s.UserB));

        await XacNhanAsync(clock, donId, s.UserA);
        await Assert.ThrowsAsync<ConflictException>(() => TaoLinkAsync(clock, donId, s.UserA));
    }

    [Fact]
    public async Task Tao_link_khi_con_duoi_1_phut_thi_409()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        clock.Advance(TimeSpan.FromMinutes(9.5));

        await Assert.ThrowsAsync<ConflictException>(() => TaoLinkAsync(clock, donId, s.UserA));
    }

    [Fact]
    public async Task IPN_thanh_cong_thi_don_Confirmed_ghe_Booked()
    {
        var clock = Clock();
        var (donId, txnRef, s) = await DatVaTaoLinkAsync(clock);

        var kq = await IpnAsync(clock, KetQuaVnPay(txnRef, 2 * Price));

        Assert.Equal("00", kq.RspCode);
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Confirmed, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
        Assert.All(await db.ShowtimeSeats.Where(g => g.ReservationId == donId).ToListAsync(),
            g => Assert.Equal(SeatStatus.Booked, g.Status));
        var payment = await db.Payments.SingleAsync(p => p.TxnRef == txnRef);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("14000001", payment.ProviderTransactionNo);
    }

    [Fact]
    public async Task IPN_sai_chu_ky_thi_97_va_DB_khong_doi()
    {
        var clock = Clock();
        var (donId, txnRef, _) = await DatVaTaoLinkAsync(clock);
        var gia = KetQuaVnPay(txnRef, 2 * Price, responseCode: "24");
        gia["vnp_ResponseCode"] = "00"; // kẻ gian sửa "khách huỷ" thành "thành công" sau khi VNPay đã ký
        gia["vnp_TransactionStatus"] = "00";

        var kq = await IpnAsync(clock, gia);

        Assert.Equal("97", kq.RspCode);
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
        Assert.Equal(PaymentStatus.Pending, (await db.Payments.SingleAsync(p => p.TxnRef == txnRef)).Status);
    }

    [Fact]
    public async Task IPN_sai_so_tien_thi_04()
    {
        var clock = Clock();
        var (donId, txnRef, _) = await DatVaTaoLinkAsync(clock);

        // Chữ ký hợp lệ nhưng số tiền khác đơn (vd. ai đó tạo link riêng 1.000đ với cùng TxnRef)
        var kq = await IpnAsync(clock, KetQuaVnPay(txnRef, 1000));

        Assert.Equal("04", kq.RspCode);
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
    }

    [Fact]
    public async Task IPN_khong_thay_TxnRef_thi_01()
    {
        var clock = Clock();
        await DatVaTaoLinkAsync(clock);

        var kq = await IpnAsync(clock, KetQuaVnPay("khong-ton-tai", 2 * Price));

        Assert.Equal("01", kq.RspCode);
    }

    [Fact]
    public async Task IPN_goi_2_lan_thi_lan_2_tra_02_va_chi_confirm_1_lan()
    {
        var clock = Clock();
        var (donId, txnRef, _) = await DatVaTaoLinkAsync(clock);
        var ketQua = KetQuaVnPay(txnRef, 2 * Price);

        var lan1 = await IpnAsync(clock, ketQua);
        clock.Advance(TimeSpan.FromMinutes(1));
        var lan2 = await IpnAsync(clock, ketQua);

        Assert.Equal("00", lan1.RspCode);
        Assert.Equal("02", lan2.RspCode);
        await using var db = NewDb();
        var don = await db.Reservations.SingleAsync(r => r.Id == donId);
        Assert.Equal(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc), don.ConfirmedAt); // giờ của lần 1, lần 2 không ghi đè
    }

    [Fact]
    public async Task Muoi_IPN_song_song_chi_dung_1_lan_xu_ly()
    {
        var clock = Clock();
        var (donId, txnRef, _) = await DatVaTaoLinkAsync(clock);
        var ketQua = KetQuaVnPay(txnRef, 2 * Price);

        // Mỗi task một DbContext riêng (giống 10 request thật), cùng lúc đọc thấy Payment Pending
        var ketQuas = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => IpnAsync(clock, ketQua))));

        Assert.Equal(1, ketQuas.Count(k => k.RspCode == "00"));
        Assert.Equal(9, ketQuas.Count(k => k.RspCode == "02"));
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Confirmed, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
    }

    [Fact]
    public async Task Thanh_toan_that_bai_thi_Payment_Failed_don_van_Pending()
    {
        var clock = Clock();
        var (donId, txnRef, s) = await DatVaTaoLinkAsync(clock);

        var kq = await IpnAsync(clock, KetQuaVnPay(txnRef, 2 * Price, responseCode: "24")); // 24 = khách huỷ giao dịch

        Assert.Equal("00", kq.RspCode); // đã nhận và ghi kết quả → VNPay không cần gọi lại
        Assert.Equal(PaymentStatus.Failed, kq.PaymentStatus);
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
        // Còn hạn giữ ghế thì khách thử thanh toán lại được
        var linkMoi = await TaoLinkAsync(clock, donId, s.UserA);
        Assert.NotNull(linkMoi.PaymentUrl);
    }

    [Fact]
    public async Task Tra_tien_khi_don_da_het_han_thi_RefundRequired()
    {
        var clock = Clock();
        var (donId, txnRef, _) = await DatVaTaoLinkAsync(clock);
        clock.Advance(TimeSpan.FromMinutes(11)); // IPN tới trễ, đơn đã quá hạn giữ ghế

        var kq = await IpnAsync(clock, KetQuaVnPay(txnRef, 2 * Price));

        Assert.Equal("00", kq.RspCode);
        Assert.Equal(PaymentStatus.RefundRequired, kq.PaymentStatus);
        await using var db = NewDb();
        Assert.NotEqual(ReservationStatus.Confirmed, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
        Assert.Equal(PaymentStatus.RefundRequired, (await db.Payments.SingleAsync(p => p.TxnRef == txnRef)).Status);
    }
}
