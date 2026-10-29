using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Features.Reservations.Expire;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
namespace MovieReservation.IntegrationTests;
/// <summary>
/// Kiểm tra job dọn đơn quá hạn bằng cách gọi thẳng handler với FakeTimeProvider (tua giờ tức thì,
/// không phải chờ 30 giây thật của job). 4 ca:
/// 1. Đơn quá hạn → Expired, mọi ghế về Available.
/// 2. Đơn còn hạn → không bị dọn, ghế vẫn Held.
/// 3. Đơn đã Confirmed (dù quá ExpiresAt) → không bị dọn, ghế vẫn Booked.
/// 4. Đơn A hết hạn nhưng job chưa chạy, B lấy được ghế nhờ lazy expiry (DB vẫn ghi Held cho A, chỉ coi như trống)
///    → job dọn: A Expired, ghế vẫn Held cho B — không nhả nhầm ghế người khác.
/// </summary>
public class ExpireReservationsTests : IAsyncLifetime
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

    [Fact]
    public async Task Don_qua_han_thi_Expired_va_nha_ghe()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        int donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        clock.Advance(TimeSpan.FromMinutes(16)); // quá hạn 6 phút
        int soDon = await DonDepAsync(clock);
        Assert.Equal(1, soDon);

        await using var db = new AppDbContext(_options);
        var don = await db.Reservations.FindAsync(donId);
        Assert.NotNull(don);
        Assert.Equal(ReservationStatus.Expired, don.Status);
        var cacGhe = await db.ShowtimeSeats.Where(ss => showtimeSeatIds.Contains(ss.Id)).ToListAsync();
        Assert.Equal(showtimeSeatIds.Count, cacGhe.Count);
        Assert.All(cacGhe, ghe =>
        {
            Assert.Null(ghe.ReservationId);
            Assert.Null(ghe.HeldUntil);
            Assert.Equal(SeatStatus.Available, ghe.Status);
        });
    }
    [Fact]
    public async Task Don_con_han_thi_khong_bi_dung_toi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        int donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        clock.Advance(TimeSpan.FromMinutes(9));
        int soDon = await DonDepAsync(clock);
        Assert.Equal(0, soDon);

        await using var db = new AppDbContext(_options);
        var don = await db.Reservations.FindAsync(donId);
        Assert.NotNull(don);
        Assert.Equal(ReservationStatus.Pending, don.Status);
        var cacGhe = await db.ShowtimeSeats.Where(ss => showtimeSeatIds.Contains(ss.Id)).ToListAsync();
        Assert.Equal(showtimeSeatIds.Count, cacGhe.Count);
        Assert.All(cacGhe, ghe =>
        {
            Assert.Equal(donId, ghe.ReservationId);
            Assert.NotNull(ghe.HeldUntil);
            Assert.Equal(SeatStatus.Held, ghe.Status);
        });
    }
    [Fact]
    public async Task Don_da_xac_nhan_khong_bi_dung_toi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        int donId = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        var loi = await XacNhanAsync(clock, donId, userIds[0]);
        Assert.Null(loi);
        clock.Advance(TimeSpan.FromMinutes(16)); // quá hạn 6 phút
        int soDon = await DonDepAsync(clock);
        Assert.Equal(0, soDon);

        await using var db = new AppDbContext(_options);
        var don = await db.Reservations.FindAsync(donId);
        Assert.NotNull(don);
        Assert.Equal(ReservationStatus.Confirmed, don.Status);
        var cacGhe = await db.ShowtimeSeats.Where(ss => showtimeSeatIds.Contains(ss.Id)).ToListAsync();
        Assert.Equal(showtimeSeatIds.Count, cacGhe.Count);
        Assert.All(cacGhe, ghe =>
        {
            Assert.Equal(donId, ghe.ReservationId);
            Assert.Null(ghe.HeldUntil);
            Assert.Equal(SeatStatus.Booked, ghe.Status);
        });
    }
    [Fact]
    public async Task Ghe_da_sang_tay_B_thi_don_dep_khong_nha_ghe_cua_B()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        int donIdA = await DatGheAsync(clock, userIds[0], showtimeId, showtimeSeatIds);
        clock.Advance(TimeSpan.FromMinutes(16)); // quá hạn 6 phút

        int donIdB = await DatGheAsync(clock, userIds[1], showtimeId, showtimeSeatIds);

        int soDon = await DonDepAsync(clock);
        Assert.Equal(1, soDon);
        await using var db = new AppDbContext(_options);
        var donA = await db.Reservations.FindAsync(donIdA);
        var donB = await db.Reservations.FindAsync(donIdB);
        Assert.NotNull(donA);
        Assert.NotNull(donB);
        Assert.Equal(ReservationStatus.Expired, donA.Status);
        Assert.Equal(ReservationStatus.Pending, donB.Status);

        var cacGhe = await db.ShowtimeSeats.Where(ss => showtimeSeatIds.Contains(ss.Id)).ToListAsync();
        Assert.Equal(showtimeSeatIds.Count, cacGhe.Count);
        Assert.All(cacGhe, ghe =>
        {
            Assert.Equal(donIdB, ghe.ReservationId);
            Assert.NotNull(ghe.HeldUntil);
            Assert.Equal(SeatStatus.Held, ghe.Status);
        });
    }
}
