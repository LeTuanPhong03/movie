using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Application.Features.Reservations.GetById;
using MovieReservation.Application.Features.Reservations.GetMine;
using MovieReservation.Domain.Enums;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Kiểm tra lịch sử đơn và xem chi tiết đơn: chỉ thấy đơn của mình (IDOR → 404), Admin thấy mọi đơn,
/// đơn đã huỷ vẫn hiện đủ ghế (nhờ bản chụp ReservationSeats), đơn Pending quá hạn hiện là Expired và lọc đúng.
/// </summary>
public class ReservationQueryTests : PostgresTestBase
{
    private async Task<PagedResult<ReservationDto>> LichSuAsync(
        TimeProvider clock, int userId, ReservationStatus? status = null)
    {
        await using var db = NewDb();
        return await new GetMyReservationsQueryHandler(db, clock)
            .Handle(new GetMyReservationsQuery(userId, status), CancellationToken.None);
    }

    private async Task<ReservationDto> ChiTietAsync(
        TimeProvider clock, int reservationId, int userId, bool isAdmin = false)
    {
        await using var db = NewDb();
        return await new GetReservationByIdQueryHandler(db, clock)
            .Handle(new GetReservationByIdQuery(reservationId, userId, isAdmin), CancellationToken.None);
    }

    [Fact]
    public async Task Lich_su_chi_co_don_cua_minh_moi_nhat_truoc()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var don1 = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        clock.Advance(TimeSpan.FromMinutes(1));
        var don2 = await DatAsync(clock, s.UserA, s.Show2, [s.Show2Seats[0], s.Show2Seats[1]]);
        await DatAsync(clock, s.UserB, s.Show1, [s.Show1Seats[1]]);

        var ketQua = await LichSuAsync(clock, s.UserA);

        Assert.Equal(2, ketQua.TotalCount);
        Assert.Equal([don2, don1], ketQua.Items.Select(d => d.Id));
        Assert.Equal("Phim B", ketQua.Items[0].MovieTitle);
        Assert.Equal(2, ketQua.Items[0].Seats.Count);
        Assert.Equal(Show2StartUtc.AddMinutes(90), ketQua.Items[0].MovieEndAt);
    }

    [Fact]
    public async Task Don_da_huy_van_hien_du_ghe()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donId = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]]);
        await HuyAsync(clock, donId, s.UserA);

        var don = await ChiTietAsync(clock, donId, s.UserA);

        // Ghế ở ShowtimeSeats đã bị nhả (ReservationId = null), nhưng lịch sử vẫn nhớ A1, A2
        Assert.Equal(ReservationStatus.Cancelled, don.Status);
        Assert.Equal(["A1", "A2"], don.Seats.Select(g => g.Row + g.Number));
    }

    [Fact]
    public async Task Don_Pending_qua_han_hien_la_Expired_va_loc_dung()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donCu = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        clock.Advance(TimeSpan.FromMinutes(11)); // đơn cũ quá hạn, job CHƯA chạy → DB vẫn Pending
        var donMoi = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[1]]);

        var pending = await LichSuAsync(clock, s.UserA, ReservationStatus.Pending);
        var expired = await LichSuAsync(clock, s.UserA, ReservationStatus.Expired);

        Assert.Equal([donMoi], pending.Items.Select(d => d.Id));
        Assert.Equal([donCu], expired.Items.Select(d => d.Id));
        Assert.Equal(ReservationStatus.Expired, expired.Items[0].Status);
    }

    [Fact]
    public async Task Xem_don_cua_nguoi_khac_thi_404_admin_thi_xem_duoc()
    {
        var clock = Clock();
        var s = await SeedAsync();
        var donA = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);

        // IDOR: B đổi Id trên URL thành đơn của A → 404, giống hệt Id không tồn tại
        await Assert.ThrowsAsync<NotFoundException>(() => ChiTietAsync(clock, donA, s.UserB));
        await Assert.ThrowsAsync<NotFoundException>(() => ChiTietAsync(clock, 999999, s.UserB));

        var donQuaAdmin = await ChiTietAsync(clock, donA, s.UserB, isAdmin: true);
        Assert.Equal(donA, donQuaAdmin.Id);
    }
}
