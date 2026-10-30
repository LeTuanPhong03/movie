using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Reservations.Cancel;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Features.Reservations.Expire;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
namespace MovieReservation.IntegrationTests;
/// <summary>
/// Kiểm tra huỷ đơn bằng cách gọi thẳng handler với FakeTimeProvider: chủ đơn huỷ được Pending lẫn Confirmed
/// (ghế về Available); người khác nhận 404 còn Admin huỷ được mọi đơn; suất đã chiếu, đơn Expired, huỷ lần 2 → 409.
/// Mọi ca bị từ chối đều kiểm DB không đổi, vì từ chối đúng lỗi mà vẫn sửa DB thì vẫn là bug.
/// </summary>
public class CancelReservationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17").Build();

    private DbContextOptions<AppDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var db = new AppDbContext(_options);
        await db.Database.MigrateAsync();
    }
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private async Task<(List<int> UserIds, int ShowtimeId, List<int> ShowtimeSeatIds)> SeedAsync()
    {
        await using var db = new AppDbContext(_options);
        var movie = new Movie { Title = "Phim test", DurationMinutes = 120, Genre = "Action" };
        var theater = new Theater { Name = "Phòng test" };
        theater.GenerateSeats(rows: 2, seatsPerRow: 3);
        var userA = new User { FullName = "Nguyen Van A", Email = "nguyenvana@example.com", PasswordHash = "test" };
        var userB = new User { FullName = "Nguyen Van B", Email = "nguyenvanb@example.com", PasswordHash = "test" };
        var showtime = new Showtime { Movie = movie, Theater = theater, StartAt = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc), Price = 90000 };
        db.Movies.Add(movie);
        db.Theaters.Add(theater);
        db.Users.Add(userA);
        db.Users.Add(userB);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();
        showtime.GenerateShowtimeSeats(theater.Seats);
        await db.SaveChangesAsync();
        return (new List<int> { userA.Id, userB.Id }, showtime.Id, showtime.ShowtimeSeats.Select(ss => ss.Id).ToList());
    }

    private async Task<int> DatGheAsync(TimeProvider clock, int userId, int showtimeId, List<int> showtimeSeatIds)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CreateReservationCommandHandler(db, clock);
        var don = await handler.Handle(
            new CreateReservationCommand(userId, showtimeId, showtimeSeatIds), CancellationToken.None);
        return don.Id;
    }

    private async Task<Exception?> XacNhanAsync(TimeProvider clock, int reservationId, int userId)
    {
        await using var db = new AppDbContext(_options);
        var handler = new ConfirmReservationCommandHandler(db, clock);
        try
        {
            await handler.Handle(new ConfirmReservationCommand(reservationId, userId), CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    // Giống 1 vòng chạy của job: trả số đơn đã chuyển sang Expired
    private async Task<int> DonDepAsync(TimeProvider clock)
    {
        await using var db = new AppDbContext(_options);
        var handler = new ExpireReservationsCommandHandler(db, clock);
        return await handler.Handle(new ExpireReservationsCommand(), CancellationToken.None);
    }

    // null = huỷ thành công; khác null = lỗi để Assert kiểm đúng loại
    private async Task<Exception?> HuyAsync(TimeProvider clock, int reservationId, int userId, bool isAdmin = false)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CancelReservationCommandHandler(db, clock);
        try
        {
            await handler.Handle(new CancelReservationCommand(reservationId, userId, isAdmin), CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    // Suất trong SeedAsync chiếu lúc 2030-01-01 18:00 UTC → mọi test đặt đồng hồ 17:00;
    // muốn "suất đã chiếu" thì tua quá 1 tiếng.
    private static FakeTimeProvider TaoDongHo() => new(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));

    // Đọc đơn + toàn bộ ghế (lọc theo Id ghế — không bao giờ đổi — chứ không theo ReservationId đã bị xoá khi huỷ)
    private async Task<(Reservation Don, List<ShowtimeSeat> Ghe)> DocDbAsync(int reservationId, List<int> showtimeSeatIds)
    {
        await using var db = new AppDbContext(_options);
        var don = await db.Reservations.SingleAsync(r => r.Id == reservationId);
        var ghe = await db.ShowtimeSeats.Where(ss => showtimeSeatIds.Contains(ss.Id)).ToListAsync();
        Assert.Equal(showtimeSeatIds.Count, ghe.Count);
        return (don, ghe);
    }

    private static void GheDaNha(List<ShowtimeSeat> ghe) => Assert.All(ghe, g =>
    {
        Assert.Equal(SeatStatus.Available, g.Status);
        Assert.Null(g.HeldUntil);
        Assert.Null(g.ReservationId);
    });

    [Fact]
    public async Task Chu_huy_don_Pending_thi_Cancelled_va_nha_ghe()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);

        Assert.Null(await HuyAsync(clock, donId, userIds[0]));

        // Sau khi huỷ: đơn Cancelled + CancelledAt = giờ huỷ; ghế Held → Available
        var (don, ghe) = await DocDbAsync(donId, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Cancelled, don.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, don.CancelledAt);
        GheDaNha(ghe);
    }

    [Fact]
    public async Task Chu_huy_don_Confirmed_thi_Cancelled_va_nha_ghe()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        Assert.Null(await XacNhanAsync(clock, donId, userIds[0]));

        Assert.Null(await HuyAsync(clock, donId, userIds[0]));

        // Sau khi huỷ: đơn Cancelled; ghế Booked → Available
        var (don, ghe) = await DocDbAsync(donId, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Cancelled, don.Status);
        Assert.NotNull(don.CancelledAt);
        GheDaNha(ghe);
    }

    [Fact]
    public async Task Nguoi_khac_huy_don_thi_404_va_DB_khong_doi()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        Assert.Null(await XacNhanAsync(clock, donA, userIds[0]));

        var loi = await HuyAsync(clock, donA, userIds[1]);

        // 404 không phải 403: không lộ "đơn này có tồn tại". Sau khi bị từ chối: DB y như trước
        Assert.IsType<NotFoundException>(loi);
        var (don, ghe) = await DocDbAsync(donA, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Confirmed, don.Status);
        Assert.Null(don.CancelledAt);
        Assert.All(ghe, g =>
        {
            Assert.Equal(SeatStatus.Booked, g.Status);
            Assert.Equal(donA, g.ReservationId);
        });
    }

    [Fact]
    public async Task Admin_huy_don_cua_nguoi_khac_thi_thanh_cong()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        Assert.Null(await XacNhanAsync(clock, donA, userIds[0]));

        // Người gọi là B (KHÔNG phải chủ) nhưng là Admin → chỉ vế "OR @isAdmin" mới cho qua
        Assert.Null(await HuyAsync(clock, donA, userIds[1], isAdmin: true));

        var (don, ghe) = await DocDbAsync(donA, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Cancelled, don.Status);
        GheDaNha(ghe);
    }

    [Fact]
    public async Task Suat_da_chieu_thi_409_va_DB_khong_doi()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        Assert.Null(await XacNhanAsync(clock, donId, userIds[0]));
        clock.Advance(TimeSpan.FromHours(2)); // 19:00, suất đã chiếu lúc 18:00

        // Admin cũng phải theo luật "suất chưa chiếu"
        var loi = await HuyAsync(clock, donId, userIds[0], isAdmin: true);

        Assert.IsType<ConflictException>(loi);
        var (don, ghe) = await DocDbAsync(donId, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Confirmed, don.Status);
        Assert.Null(don.CancelledAt);
        Assert.All(ghe, g =>
        {
            Assert.Equal(SeatStatus.Booked, g.Status);
            Assert.Null(g.HeldUntil); // Confirm đã xoá HeldUntil
            Assert.Equal(donId, g.ReservationId);
        });
    }

    [Fact]
    public async Task Huy_don_da_Expired_thi_409()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        // Chỉ tua giờ thì DB vẫn Pending (lazy expiry không ghi DB) → cho job chạy để đơn Expired THẬT
        clock.Advance(TimeSpan.FromMinutes(11));
        Assert.Equal(1, await DonDepAsync(clock));

        var loi = await HuyAsync(clock, donId, userIds[0]);

        Assert.IsType<ConflictException>(loi);
        var (don, ghe) = await DocDbAsync(donId, showtimeSeatIds);
        Assert.Equal(ReservationStatus.Expired, don.Status);
        Assert.Null(don.CancelledAt);
        GheDaNha(ghe); // job đã nhả từ trước, huỷ thất bại không đụng gì thêm
    }

    [Fact]
    public async Task Huy_2_lan_thi_lan_2_bi_409()
    {
        var clock = TaoDongHo();
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);

        var loi1 = await HuyAsync(clock, donId, userIds[0]);
        var loi2 = await HuyAsync(clock, donId, userIds[0]);

        Assert.Null(loi1);
        Assert.IsType<ConflictException>(loi2);
    }
}
