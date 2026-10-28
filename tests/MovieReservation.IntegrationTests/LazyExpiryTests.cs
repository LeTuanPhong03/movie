using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Domain.Entities;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Time.Testing;
namespace MovieReservation.IntegrationTests;
/// <summary>
/// Kiểm tra lazy expiry: A đặt ghế, B đặt cùng ghế trong 9 phút → bị chặn, tua thêm 2 phút → B đặt được. Mỗi task 1 DbContext riêng để giống request thật.
/// </summary>
public class LazyExpiryTests : IAsyncLifetime
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
        var userA = new User { FullName = "Nguyen Van A", Email = "nguyenvanA@example.com", PasswordHash = "test" };
        var userB = new User { FullName = "Nguyen Van B", Email = "nguyenvanb@example.com", PasswordHash = "test" };
        var showtime = new Showtime { Movie = movie, Theater = theater, StartAt = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc), Price = 90000 };
        db.Movies.Add(movie);
        db.Theaters.Add(theater);
        db.Users.Add(userA);
        db.Users.Add(userB);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();
        showtime.GenerateShowtimeSeats(theater.Seats); // sinh ShowtimeSeat cho mọi ghế của phòng
        await db.SaveChangesAsync();
        return (new List<int> { userA.Id, userB.Id }, showtime.Id, showtime.ShowtimeSeats.Select(ss => ss.Id).ToList());
    }
    private async Task<Exception?> DatGheAsync(TimeProvider clock, int userId, int showtimeId, List<int> showtimeSeatIds)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CreateReservationCommandHandler(db, clock);
        try
        {
            await handler.Handle(
                new CreateReservationCommand(userId, showtimeId, showtimeSeatIds), CancellationToken.None);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
    [Fact]
    public async Task Ghe_giu_qua_10_phut_thi_nguoi_khac_dat_duoc()
    {
        // Arrange
        var (userIds, showtimeId, showtimeSeatIds) = await SeedAsync();
        var clock = new FakeTimeProvider();
        var ghe = new List<int> { showtimeSeatIds[0] };

        // Act 1: A đặt ghế → phải thành công
        var loiA = await DatGheAsync(clock, userIds[0], showtimeId, ghe);
        Assert.Null(loiA);   // null = không có lỗi = đặt được

        // Act 2: tua 9 phút, B đặt cùng ghế → phải bị chặn
        clock.Advance(TimeSpan.FromMinutes(9));
        var loiB1 = await DatGheAsync(clock, userIds[1], showtimeId, ghe);
        Assert.IsType<ConflictException>(loiB1);

        // Act 3: tua thêm 2 phút, B đặt lại → phải thành công
        clock.Advance(TimeSpan.FromMinutes(2));
        var loiB2 = await DatGheAsync(clock, userIds[1], showtimeId, ghe);
        Assert.Null(loiB2);
        // Assert: đọc lại từ DB bằng context mới, xem B có đặt được ghế hay không
        await using (var db = new AppDbContext(_options))
        {
            var don = await db.ShowtimeSeats.Where(ss => ss.Id == ghe[0]).Select(ss => ss.Reservation).SingleAsync();
            Assert.NotNull(don);
            Assert.Equal(userIds[1], don.UserId);
        }
    }
    
}