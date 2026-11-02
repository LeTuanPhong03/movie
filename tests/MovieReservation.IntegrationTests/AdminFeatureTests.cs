using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Reports.Capacity;
using MovieReservation.Application.Features.Reservations.GetAll;
using MovieReservation.Application.Features.Theaters.GetAll;
using MovieReservation.Application.Features.Theaters.GetById;
using MovieReservation.Application.Features.Users.ChangeRole;
using MovieReservation.Application.Features.Users.GetUsers;
using MovieReservation.Domain.Enums;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Các chức năng Admin còn thiếu so với đề bài: đổi quyền user (không tự đổi chính mình, user không có → 404),
/// tìm user, xem tất cả đơn (lọc theo suất / user / trạng thái thật), báo cáo công suất (Held quá hạn tính là trống),
/// xem phòng theo Id (Rows/SeatsPerRow tính lại từ ghế).
/// </summary>
public class AdminFeatureTests : PostgresTestBase
{
    // ── Đổi quyền ──

    [Fact]
    public async Task Admin_nang_user_len_Admin_thi_DB_doi_Role()
    {
        var s = await SeedAsync();
        await using (var db = NewDb())
            await new ChangeUserRoleCommandHandler(db)
                .Handle(new ChangeUserRoleCommand(s.UserB, UserRole.Admin, CurrentUserId: s.UserA), CancellationToken.None);

        await using var check = NewDb();
        Assert.Equal(UserRole.Admin, (await check.Users.SingleAsync(u => u.Id == s.UserB)).Role);
    }

    [Fact]
    public async Task Admin_tu_doi_quyen_chinh_minh_bi_tu_choi_va_khong_doi_gi()
    {
        var s = await SeedAsync();
        await using (var db = NewDb())
        {
            await db.Users.Where(u => u.Id == s.UserA).ExecuteUpdateAsync(x => x.SetProperty(u => u.Role, UserRole.Admin));
            await Assert.ThrowsAsync<ConflictException>(() => new ChangeUserRoleCommandHandler(db)
                .Handle(new ChangeUserRoleCommand(s.UserA, UserRole.Customer, CurrentUserId: s.UserA), CancellationToken.None));
        }

        await using var check = NewDb();
        Assert.Equal(UserRole.Admin, (await check.Users.SingleAsync(u => u.Id == s.UserA)).Role);
    }

    // A = Owner, B = Admin thường, khách C (thêm mới)
    private async Task<(int Owner, int Admin, int Khach)> OwnerAdminKhachAsync()
    {
        var s = await SeedAsync();
        await using var db = NewDb();
        await db.Users.Where(u => u.Id == s.UserA)
            .ExecuteUpdateAsync(x => x.SetProperty(u => u.Role, UserRole.Admin).SetProperty(u => u.IsOwner, true));
        await db.Users.Where(u => u.Id == s.UserB).ExecuteUpdateAsync(x => x.SetProperty(u => u.Role, UserRole.Admin));
        var khach = new Domain.Entities.User { FullName = "C", Email = "c@example.com", PasswordHash = "x" };
        db.Users.Add(khach);
        await db.SaveChangesAsync();
        return (s.UserA, s.UserB, khach.Id);
    }

    private async Task DoiQuyenAsync(int userId, UserRole role, int nguoiGoi)
    {
        await using var db = NewDb();
        await new ChangeUserRoleCommandHandler(db).Handle(new ChangeUserRoleCommand(userId, role, nguoiGoi), CancellationToken.None);
    }

    private async Task<UserRole> RoleAsync(int userId)
    {
        await using var db = NewDb();
        return await db.Users.Where(u => u.Id == userId).Select(u => u.Role).SingleAsync();
    }

    [Fact]
    public async Task Admin_thuong_ha_Owner_bi_tu_choi_Owner_van_la_Admin()
    {
        var (owner, admin, _) = await OwnerAdminKhachAsync();

        await Assert.ThrowsAsync<ConflictException>(() => DoiQuyenAsync(owner, UserRole.Customer, nguoiGoi: admin));

        Assert.Equal(UserRole.Admin, await RoleAsync(owner));
    }

    [Fact]
    public async Task Admin_thuong_nang_duoc_khach_nhung_khong_ha_duoc_Admin_khac()
    {
        var (_, admin, khach) = await OwnerAdminKhachAsync();

        await DoiQuyenAsync(khach, UserRole.Admin, nguoiGoi: admin);
        Assert.Equal(UserRole.Admin, await RoleAsync(khach));

        await Assert.ThrowsAsync<ForbiddenException>(() => DoiQuyenAsync(khach, UserRole.Customer, nguoiGoi: admin));
        Assert.Equal(UserRole.Admin, await RoleAsync(khach));
    }

    [Fact]
    public async Task Owner_ha_duoc_Admin_khac()
    {
        var (owner, admin, _) = await OwnerAdminKhachAsync();

        await DoiQuyenAsync(admin, UserRole.Customer, nguoiGoi: owner);

        Assert.Equal(UserRole.Customer, await RoleAsync(admin));
    }

    [Fact]
    public async Task Doi_quyen_user_khong_ton_tai_tra_404()
    {
        var s = await SeedAsync();
        await using var db = NewDb();
        await Assert.ThrowsAsync<NotFoundException>(() => new ChangeUserRoleCommandHandler(db)
            .Handle(new ChangeUserRoleCommand(99999, UserRole.Admin, CurrentUserId: s.UserA), CancellationToken.None));
    }

    [Fact]
    public async Task Tim_user_theo_email_khong_phan_biet_hoa_thuong()
    {
        var s = await SeedAsync();
        await using var db = NewDb();
        var kq = await new GetUsersQueryHandler(db).Handle(new GetUsersQuery("B@EXAMPLE"), CancellationToken.None);

        Assert.Equal(1, kq.TotalCount);
        Assert.Equal(s.UserB, kq.Items[0].Id);
        Assert.Equal("Customer", kq.Items[0].Role);
    }

    // ── Tất cả đơn ──

    private async Task<List<int>> DanhSachDonAsync(GetAllReservationsQuery query, TimeProvider clock)
    {
        await using var db = NewDb();
        var kq = await new GetAllReservationsQueryHandler(db, clock).Handle(query, CancellationToken.None);
        return kq.Items.Select(x => x.Reservation.Id).ToList();
    }

    [Fact]
    public async Task Admin_thay_don_cua_moi_nguoi_moi_nhat_truoc_kem_email_nguoi_dat()
    {
        var s = await SeedAsync();
        var clock = Clock();
        var donA = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        clock.Advance(TimeSpan.FromSeconds(1));
        var donB = await DatAsync(clock, s.UserB, s.Show2, [s.Show2Seats[0]]);

        await using var db = NewDb();
        var kq = await new GetAllReservationsQueryHandler(db, clock).Handle(new GetAllReservationsQuery(), CancellationToken.None);

        Assert.Equal(2, kq.TotalCount);
        Assert.Equal([donB, donA], kq.Items.Select(x => x.Reservation.Id));
        Assert.Equal(["b@example.com", "a@example.com"], kq.Items.Select(x => x.UserEmail));
        Assert.Single(kq.Items[0].Reservation.Seats);
    }

    [Fact]
    public async Task Loc_theo_suat_user_va_trang_thai_that()
    {
        var s = await SeedAsync();
        var clock = Clock();
        var donA1 = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        var donB1 = await DatAsync(clock, s.UserB, s.Show1, [s.Show1Seats[1]]);
        await XacNhanAsync(clock, donB1, s.UserB);
        var donA2 = await DatAsync(clock, s.UserA, s.Show2, [s.Show2Seats[0]]);

        Assert.Equal(new[] { donB1, donA1 }.Order(), (await DanhSachDonAsync(new(ShowtimeId: s.Show1), clock)).Order());
        Assert.Equal(new[] { donA2, donA1 }.Order(), (await DanhSachDonAsync(new(UserId: s.UserA), clock)).Order());
        Assert.Equal([donB1], await DanhSachDonAsync(new(Status: ReservationStatus.Confirmed), clock));

        // Quá 10 phút: 2 đơn Pending của A trong DB vẫn Pending nhưng lọc theo trạng thái thật là Expired
        clock.Advance(TimeSpan.FromMinutes(Domain.Entities.Reservation.HoldMinutes + 1));
        Assert.Empty(await DanhSachDonAsync(new(Status: ReservationStatus.Pending), clock));
        Assert.Equal(new[] { donA2, donA1 }.Order(), (await DanhSachDonAsync(new(Status: ReservationStatus.Expired), clock)).Order());
    }

    // ── Công suất ──

    private async Task<CapacityReportDto> CongSuatAsync(TimeProvider clock)
    {
        await using var db = NewDb();
        // Suất 1 = 2030-01-02 01:00 giờ VN, suất 2 = 2030-01-03 01:00 giờ VN
        return await new GetCapacityReportQueryHandler(db, clock)
            .Handle(new GetCapacityReportQuery(new DateOnly(2030, 1, 2), new DateOnly(2030, 1, 3)), CancellationToken.None);
    }

    [Fact]
    public async Task Cong_suat_dem_ghe_ban_giu_trong_tung_suat()
    {
        var s = await SeedAsync();
        var clock = Clock();
        var don1 = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1], s.Show1Seats[2]]);
        await XacNhanAsync(clock, don1, s.UserA);
        await DatAsync(clock, s.UserB, s.Show1, [s.Show1Seats[3]]);

        var bc = await CongSuatAsync(clock);

        Assert.Equal(2, bc.Rows.Count);
        var suat1 = bc.Rows.Single(r => r.ShowtimeId == s.Show1);
        Assert.Equal((6, 3, 1, 2, 50.0), (suat1.TotalSeats, suat1.BookedSeats, suat1.HeldSeats, suat1.AvailableSeats, suat1.OccupancyPercent));
        var suat2 = bc.Rows.Single(r => r.ShowtimeId == s.Show2);
        Assert.Equal((0, 6, 0.0), (suat2.BookedSeats, suat2.AvailableSeats, suat2.OccupancyPercent));
        Assert.Equal((12, 3, 25.0), (bc.TotalSeats, bc.BookedSeats, bc.OccupancyPercent));
    }

    [Fact]
    public async Task Cong_suat_ghe_giu_qua_han_tinh_la_trong_du_job_chua_don()
    {
        var s = await SeedAsync();
        var clock = Clock();
        await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]]);
        clock.Advance(TimeSpan.FromMinutes(Domain.Entities.Reservation.HoldMinutes + 1));

        var suat1 = (await CongSuatAsync(clock)).Rows.Single(r => r.ShowtimeId == s.Show1);

        Assert.Equal((0, 6), (suat1.HeldSeats, suat1.AvailableSeats));
    }

    // ── Phòng chiếu ──

    [Fact]
    public async Task Xem_phong_theo_Id_tinh_lai_so_hang_va_ghe_moi_hang()
    {
        await SeedAsync();
        await using var db = NewDb();
        var theaterId = await db.Theaters.Select(t => t.Id).SingleAsync();

        var phong = await new GetTheaterByIdQueryHandler(db).Handle(new GetTheaterByIdQuery(theaterId), CancellationToken.None);

        Assert.Equal((2, 3, 6), (phong.Rows, phong.SeatsPerRow, phong.TotalSeats));
        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetTheaterByIdQueryHandler(db).Handle(new GetTheaterByIdQuery(99999), CancellationToken.None));

        var tatCa = await new GetTheatersQueryHandler(db).Handle(new GetTheatersQuery(), CancellationToken.None);
        Assert.Equal([phong], tatCa);
    }
}
