using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Common.Exceptions;
namespace MovieReservation.IntegrationTests;

/// <summary>
///Kiểm tra đặt vé khi nhiều người cùng đặt 1 ghế, trên Postgres thật. Mỗi task 1 DbContext riêng để giống request thật.
/// </summary>
public class DoubleBookingTests : IAsyncLifetime
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
    private async Task<(int UserId, int ShowtimeId, int ShowtimeSeatId)> SeedAsync()
    {
        await using var db = new AppDbContext(_options);
        var movie = new Movie { Title = "Phim test", DurationMinutes = 120, Genre = "Action" };
        var theater = new Theater { Name = "Phòng test" };
        theater.GenerateSeats(rows: 2, seatsPerRow: 3);
        var user = new User { FullName = "Nguyen Van Test", Email = "nguyenvantest@example.com", PasswordHash = "test" };
        var showtime = new Showtime { Movie = movie, Theater = theater, StartAt = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc), Price = 90000 };
        db.Movies.Add(movie);
        db.Theaters.Add(theater);
        db.Users.Add(user);
        db.Showtimes.Add(showtime);
        await db.SaveChangesAsync();
        showtime.GenerateShowtimeSeats(theater.Seats); // sinh ShowtimeSeat cho mọi ghế của phòng
        await db.SaveChangesAsync();
        return (user.Id, showtime.Id, showtime.ShowtimeSeats.First().Id);
    }
    private async Task<bool> DatGheAsync(int userId, int showtimeId, int showtimeSeatId)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CreateReservationCommandHandler(db, TimeProvider.System);
        try
        {
            await handler.Handle(
                new CreateReservationCommand(userId, showtimeId, new List<int> { showtimeSeatId }), CancellationToken.None);
            return true;
        }
        catch (ConflictException)
        {
            return false;
        }
    }

    [Fact]
    public async Task Dat_1_ghe_trong_thi_tao_1_don()
    {
        // Arrange
        var (userId, showtimeId, showtimeSeatId) = await SeedAsync();

        // Act
        await using (var db = new AppDbContext(_options))
        {
            var handler = new CreateReservationCommandHandler(db,TimeProvider.System);
            await handler.Handle(
                new CreateReservationCommand(userId, showtimeId,[showtimeSeatId] ), CancellationToken.None);
        }

        // Assert: đọc lại từ DB bằng context mới, không đọc từ bộ nhớ của context ở bước Act
        await using (var db = new AppDbContext(_options))
        {
            var soDon = await db.Reservations.CountAsync();
            Assert.Equal(1, soDon);
        }
    }
    [Fact]
    public async Task Hai_muoi_nguoi_dat_cung_1_ghe_thi_chi_1_nguoi_thanh_cong()
    {
        // Arrange
        var (userId, showtimeId, showtimeSeatId) = await SeedAsync();

        // Act
        var tasks = Enumerable.Range(0, 20).Select(_ => DatGheAsync(userId, showtimeId, showtimeSeatId)).ToArray();
        var results = await Task.WhenAll(tasks);
        var soNguoiThanhCong = results.Count(r => r);
        // Assert
        await using (var db = new AppDbContext(_options))
        {
            var soDon = await db.Reservations.CountAsync();
            Assert.Equal(soNguoiThanhCong, soDon);
            Assert.Equal(1, soNguoiThanhCong);
        }
    }
}