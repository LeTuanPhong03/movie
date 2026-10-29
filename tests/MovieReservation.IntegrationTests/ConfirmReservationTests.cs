using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
namespace MovieReservation.IntegrationTests;
/// <summary>
/// Kiểm tra Confirm nguyên tử: trong hạn thì đơn Confirmed + ghế Booked; quá hạn (kể cả khi ghế đã sang người khác),
/// đơn của người khác, hay confirm lần 2 đều bị từ chối và DB không đổi. Mỗi lần gọi 1 DbContext riêng như request thật.
/// </summary>
public class ConfirmReservationTests : IAsyncLifetime
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

    // Đặt ghế phải thành công (đây là bước Arrange), nên để exception bay ra cho test đỏ luôn nếu hỏng
    private async Task<int> DatGheAsync(TimeProvider clock, int userId, int showtimeId, List<int> showtimeSeatIds)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CreateReservationCommandHandler(db, clock);
        var don = await handler.Handle(
            new CreateReservationCommand(userId, showtimeId, showtimeSeatIds), CancellationToken.None);
        return don.Id;
    }

    // null = confirm thành công; khác null = lỗi để Assert kiểm đúng loại
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

    [Fact]
    public async Task Confirm_trong_han_thi_don_Confirmed_va_ghe_Booked()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var ghe = new List<int> { showtimeSeatIds[0], showtimeSeatIds[1] };
        var donId = await DatGheAsync(clock, userIds[0], showtimeId, ghe);

        clock.Advance(TimeSpan.FromMinutes(9));
        var loi = await XacNhanAsync(clock, donId, userIds[0]);

        Assert.Null(loi);
        await using var db = new AppDbContext(_options);
        var don = await db.Reservations.SingleAsync(r => r.Id == donId);
        Assert.Equal(ReservationStatus.Confirmed, don.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, don.ConfirmedAt);
        var cacGhe = await db.ShowtimeSeats.Where(ss => ghe.Contains(ss.Id)).ToListAsync();
        Assert.All(cacGhe, ss =>
        {
            Assert.Equal(SeatStatus.Booked, ss.Status);
            Assert.Null(ss.HeldUntil);
            Assert.Equal(donId, ss.ReservationId);
        });
    }

    [Fact]
    public async Task Confirm_sau_khi_het_han_va_ghe_da_sang_nguoi_khac_thi_bi_tu_choi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var ghe = new List<int> { showtimeSeatIds[0] };
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, ghe);

        // Quá 10 phút: lazy expiry cho B lấy ghế, nhưng đơn A trong DB vẫn Pending
        clock.Advance(TimeSpan.FromMinutes(11));
        var donB = await DatGheAsync(clock, userIds[1], showtimeId, ghe);

        var loi = await XacNhanAsync(clock, donA, userIds[0]);

        Assert.IsType<ConflictException>(loi);
        await using var db = new AppDbContext(_options);
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donA)).Status);
        // Bằng chứng không bán 2 lần: ghế vẫn Held cho B, không bị A chuyển thành Booked
        var ghe0 = await db.ShowtimeSeats.SingleAsync(ss => ss.Id == ghe[0]);
        Assert.Equal(SeatStatus.Held, ghe0.Status);
        Assert.Equal(donB, ghe0.ReservationId);
    }

    // Ca này riêng để chứng minh điều kiện ExpiresAt > now: ghế vẫn thuộc đơn A nên lớp phòng thủ 2
    // (lọc ghế theo ReservationId) KHÔNG chặn được — chỉ còn điều kiện hết hạn ở câu UPDATE đơn.
    [Fact]
    public async Task Confirm_sau_khi_het_han_du_chua_ai_lay_ghe_thi_bi_tu_choi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var ghe = new List<int> { showtimeSeatIds[0] };
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, ghe);

        clock.Advance(TimeSpan.FromMinutes(11));
        var loi = await XacNhanAsync(clock, donA, userIds[0]);

        Assert.IsType<ConflictException>(loi);
        await using var db = new AppDbContext(_options);
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donA)).Status);
        Assert.NotEqual(SeatStatus.Booked, (await db.ShowtimeSeats.SingleAsync(ss => ss.Id == ghe[0])).Status);
    }

    [Fact]
    public async Task Confirm_don_cua_nguoi_khac_thi_404_va_don_khong_doi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, new List<int> { showtimeSeatIds[0] });

        var loi = await XacNhanAsync(clock, donA, userIds[1]);

        Assert.IsType<NotFoundException>(loi);
        await using var db = new AppDbContext(_options);
        Assert.Equal(ReservationStatus.Pending, (await db.Reservations.SingleAsync(r => r.Id == donA)).Status);
    }

    [Fact]
    public async Task Confirm_lan_thu_hai_thi_bi_tu_choi()
    {
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var donA = await DatGheAsync(clock, userIds[0], showtimeId, new List<int> { showtimeSeatIds[0] });

        var loi1 = await XacNhanAsync(clock, donA, userIds[0]);
        var loi2 = await XacNhanAsync(clock, donA, userIds[0]);

        Assert.Null(loi1);
        Assert.IsType<ConflictException>(loi2);
    }
}
