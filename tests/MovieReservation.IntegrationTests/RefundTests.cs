using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Refunds.GetAll;
using MovieReservation.Application.Features.Refunds.MarkRefunded;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Tiền không được mất dấu khi huỷ vé đã thanh toán: huỷ → Payment Succeeded thành RefundRequired trong cùng transaction;
/// khách tự huỷ sát giờ (≤ 2 tiếng) bị chặn còn Admin thì được; Admin xem danh sách cần hoàn và đánh dấu đã hoàn
/// (một lần duy nhất, kể cả hai Admin bấm cùng lúc).
/// </summary>
public class RefundTests : PostgresTestBase
{
    // Đơn của A cho suất 2 (2030-01-02 18:00 UTC, còn hơn 1 ngày) đã Confirmed + có Payment Succeeded như sau IPN thành công
    private async Task<(Seeded S, int DonId, int PaymentId)> DonDaThanhToanAsync(TimeProvider clock, bool suat1 = false)
    {
        var s = await SeedAsync();
        var donId = suat1
            ? await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]])
            : await DatAsync(clock, s.UserA, s.Show2, [s.Show2Seats[0], s.Show2Seats[1]]);
        await XacNhanAsync(clock, donId, s.UserA);
        await using var db = NewDb();
        var payment = new Payment
        {
            ReservationId = donId,
            TxnRef = Guid.NewGuid().ToString("N"),
            Amount = 2 * Price,
            Status = PaymentStatus.Succeeded,
            ProviderTransactionNo = "14123456",
            CreatedAt = clock.GetUtcNow().UtcDateTime,
            CompletedAt = clock.GetUtcNow().UtcDateTime
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();
        return (s, donId, payment.Id);
    }

    private async Task<Payment> DocPaymentAsync(int paymentId)
    {
        await using var db = NewDb();
        return await db.Payments.AsNoTracking().SingleAsync(p => p.Id == paymentId);
    }

    private async Task DanhDauDaHoanAsync(TimeProvider clock, int paymentId)
    {
        await using var db = NewDb();
        await new MarkRefundedCommandHandler(db, clock).Handle(new MarkRefundedCommand(paymentId), CancellationToken.None);
    }

    [Fact]
    public async Task Huy_ve_da_thanh_toan_thi_Payment_chuyen_RefundRequired()
    {
        var clock = Clock();
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock);

        await HuyAsync(clock, donId, s.UserA);

        Assert.Equal(PaymentStatus.RefundRequired, (await DocPaymentAsync(paymentId)).Status);
    }

    [Fact]
    public async Task Khach_huy_ve_da_tra_sat_gio_bi_409_va_khong_doi_gi()
    {
        // Clock = 17:00, suất 1 lúc 18:00 → chỉ còn 1 tiếng (< 2 tiếng)
        var clock = Clock();
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock, suat1: true);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => HuyAsync(clock, donId, s.UserA));

        Assert.Contains("2 giờ", ex.Message);
        Assert.Equal(PaymentStatus.Succeeded, (await DocPaymentAsync(paymentId)).Status);
        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Confirmed, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
    }

    [Fact]
    public async Task Admin_huy_ve_da_tra_sat_gio_van_duoc_va_ghi_can_hoan()
    {
        var clock = Clock();
        var (_, donId, paymentId) = await DonDaThanhToanAsync(clock, suat1: true);

        await HuyAsync(clock, donId, userId: 99999, isAdmin: true);

        Assert.Equal(PaymentStatus.RefundRequired, (await DocPaymentAsync(paymentId)).Status);
    }

    [Fact]
    public async Task Huy_don_chua_thanh_toan_sat_gio_van_duoc()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);

        await HuyAsync(clock, donId, s.UserA);

        await using var db = NewDb();
        Assert.Equal(ReservationStatus.Cancelled, (await db.Reservations.SingleAsync(r => r.Id == donId)).Status);
    }

    [Fact]
    public async Task Danh_sach_can_hoan_co_du_thong_tin_de_hoan_tay()
    {
        var clock = Clock();
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock);
        await HuyAsync(clock, donId, s.UserA);

        await using var db = NewDb();
        var kq = await new GetRefundsQueryHandler(db).Handle(new GetRefundsQuery(), CancellationToken.None);

        var dong = Assert.Single(kq.Items);
        Assert.Equal((paymentId, donId, ReservationStatus.Cancelled, "a@example.com", 2 * Price, "14123456"),
            (dong.PaymentId, dong.ReservationId, dong.ReservationStatus, dong.UserEmail, dong.Amount, dong.ProviderTransactionNo));
        Assert.Equal("Phim B", dong.MovieTitle);
    }

    [Fact]
    public async Task Danh_dau_da_hoan_thi_Refunded_va_chuyen_sang_danh_sach_da_hoan()
    {
        var clock = Clock();
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock);
        await HuyAsync(clock, donId, s.UserA);

        await DanhDauDaHoanAsync(clock, paymentId);

        var payment = await DocPaymentAsync(paymentId);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, payment.RefundedAt);
        await using var db = NewDb();
        Assert.Empty((await new GetRefundsQueryHandler(db).Handle(new GetRefundsQuery(), CancellationToken.None)).Items);
        Assert.Single((await new GetRefundsQueryHandler(db)
            .Handle(new GetRefundsQuery(PaymentStatus.Refunded), CancellationToken.None)).Items);
    }

    [Fact]
    public async Task Danh_dau_lan_2_bi_409_khong_ton_tai_bi_404_chua_can_hoan_bi_409()
    {
        var clock = Clock();
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock);

        // Chưa huỷ → Payment vẫn Succeeded, không phải khoản cần hoàn
        await Assert.ThrowsAsync<ConflictException>(() => DanhDauDaHoanAsync(clock, paymentId));

        await HuyAsync(clock, donId, s.UserA);
        await DanhDauDaHoanAsync(clock, paymentId);
        await Assert.ThrowsAsync<ConflictException>(() => DanhDauDaHoanAsync(clock, paymentId));
        await Assert.ThrowsAsync<NotFoundException>(() => DanhDauDaHoanAsync(clock, 99999));
    }

    [Fact]
    public async Task Hai_Admin_danh_dau_da_hoan_cung_luc_chi_mot_nguoi_thanh_cong()
    {
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        var (s, donId, paymentId) = await DonDaThanhToanAsync(clock);
        await HuyAsync(clock, donId, s.UserA);

        var ketQua = await Task.WhenAll(Enumerable.Range(0, 5).Select(async _ =>
        {
            try { await DanhDauDaHoanAsync(clock, paymentId); return true; }
            catch (ConflictException) { return false; }
        }));

        Assert.Equal(1, ketQua.Count(ok => ok));
    }
}
