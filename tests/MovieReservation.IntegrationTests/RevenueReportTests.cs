using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Features.Reports.Revenue;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Báo cáo doanh thu: chỉ đơn Confirmed (Pending, Cancelled sau khi Confirm đều không tính), chia ngày theo giờ VN
/// (16:59 UTC = 23:59 VN ngày 1, 17:00 UTC = 00:00 VN ngày 2), nhóm theo phim, và tổng khớp với câu SQL viết tay.
/// </summary>
public class RevenueReportTests : PostgresTestBase
{
    private async Task<RevenueReportDto> BaoCaoAsync(DateOnly from, DateOnly to, RevenueGroupBy groupBy)
    {
        await using var db = NewDb();
        return await new GetRevenueReportQueryHandler(db)
            .Handle(new GetRevenueReportQuery(from, to, groupBy), CancellationToken.None);
    }

    // don1: A, phim A, 2 ghế, confirm 23:59 VN ngày 1   → tính, ngày 2030-01-01
    // don2: B, phim A, 1 ghế, confirm 00:00 VN ngày 2   → tính, ngày 2030-01-02
    // don3: A, phim B, 1 ghế, confirm rồi huỷ           → KHÔNG tính
    // don4: B, phim B, 1 ghế, confirm 00:05 VN ngày 2   → tính
    // don5: A, phim B, 1 ghế, chỉ Pending              → KHÔNG tính
    private async Task TaoDuLieuAsync()
    {
        var s = await SeedAsync();
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 16, 59, 0, DateTimeKind.Utc));

        var don1 = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0], s.Show1Seats[1]]);
        await XacNhanAsync(clock, don1, s.UserA);

        clock.Advance(TimeSpan.FromMinutes(1));
        var don2 = await DatAsync(clock, s.UserB, s.Show1, [s.Show1Seats[2]]);
        await XacNhanAsync(clock, don2, s.UserB);

        var don3 = await DatAsync(clock, s.UserA, s.Show2, [s.Show2Seats[0]]);
        await XacNhanAsync(clock, don3, s.UserA);
        await HuyAsync(clock, don3, s.UserA);

        clock.Advance(TimeSpan.FromMinutes(5));
        var don4 = await DatAsync(clock, s.UserB, s.Show2, [s.Show2Seats[1]]);
        await XacNhanAsync(clock, don4, s.UserB);

        await DatAsync(clock, s.UserA, s.Show2, [s.Show2Seats[2]]);
    }

    [Fact]
    public async Task Nhom_theo_ngay_chia_dung_nua_dem_gio_VN_va_bo_don_khong_Confirmed()
    {
        await TaoDuLieuAsync();

        var bc = await BaoCaoAsync(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 2), RevenueGroupBy.Day);

        Assert.Equal(2, bc.Rows.Count);
        Assert.Equal(new RevenueRowDto("2030-01-01", 2 * Price, 1, 2), bc.Rows[0]);
        Assert.Equal(new RevenueRowDto("2030-01-02", 2 * Price, 2, 2), bc.Rows[1]);
        Assert.Equal(4 * Price, bc.TotalRevenue);
        Assert.Equal(3, bc.TotalReservations);
        Assert.Equal(4, bc.TotalTickets);
    }

    [Fact]
    public async Task Loc_mot_ngay_khong_lan_sang_ngay_ben_canh()
    {
        await TaoDuLieuAsync();

        var bc = await BaoCaoAsync(new DateOnly(2030, 1, 2), new DateOnly(2030, 1, 2), RevenueGroupBy.Day);

        // don1 confirm 23:59 VN ngày 1 → không thuộc ngày 2
        Assert.Equal(2 * Price, bc.TotalRevenue);
        Assert.Equal(2, bc.TotalReservations);
    }

    [Fact]
    public async Task Nhom_theo_phim_sap_doanh_thu_giam_dan()
    {
        await TaoDuLieuAsync();

        var bc = await BaoCaoAsync(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 2), RevenueGroupBy.Movie);

        Assert.Equal(
            [new RevenueRowDto("Phim A", 3 * Price, 2, 3), new RevenueRowDto("Phim B", 1 * Price, 1, 1)],
            bc.Rows.Select(r => r with { MovieId = null }));
        Assert.All(bc.Rows, r => Assert.NotNull(r.MovieId));
    }

    [Fact]
    public async Task Nhom_theo_phim_hai_phim_trung_ten_la_hai_dong_khac_MovieId()
    {
        var s = await SeedAsync();
        await using (var db = NewDb())
        {
            // Đổi tên phim B thành "Phim A": 2 phim khác nhau, cùng tên (vd bản gốc và bản làm lại)
            await db.Movies.Where(m => m.Title == "Phim B")
                .ExecuteUpdateAsync(x => x.SetProperty(m => m.Title, "Phim A"));
        }
        var clock = Clock();
        var don1 = await DatAsync(clock, s.UserA, s.Show1, [s.Show1Seats[0]]);
        await XacNhanAsync(clock, don1, s.UserA);
        var don2 = await DatAsync(clock, s.UserB, s.Show2, [s.Show2Seats[0]]);
        await XacNhanAsync(clock, don2, s.UserB);

        var bc = await BaoCaoAsync(new DateOnly(2030, 1, 2), new DateOnly(2030, 1, 2), RevenueGroupBy.Movie);

        Assert.Equal(2, bc.Rows.Count);
        Assert.All(bc.Rows, r => Assert.Equal("Phim A", r.Key));
        Assert.Equal(2, bc.Rows.Select(r => r.MovieId).Distinct().Count());
    }

    [Fact]
    public async Task Tong_doanh_thu_khop_cau_SQL_viet_tay()
    {
        await TaoDuLieuAsync();
        var bc = await BaoCaoAsync(new DateOnly(2030, 1, 1), new DateOnly(2030, 1, 2), RevenueGroupBy.Day);

        // Ngày VN 1/1 00:00 = 2029-12-31 17:00 UTC; ngày VN 3/1 00:00 = 2030-01-02 17:00 UTC
        await using var db = NewDb();
        var tongSql = await db.Database.SqlQuery<decimal>($"""
            SELECT COALESCE(SUM("TotalPrice"), 0) AS "Value" FROM "Reservations"
            WHERE "Status" = 'Confirmed'
              AND "ConfirmedAt" >= '2029-12-31 17:00:00+00' AND "ConfirmedAt" < '2030-01-02 17:00:00+00'
            """).SingleAsync();

        Assert.Equal(tongSql, bc.TotalRevenue);
    }
}
