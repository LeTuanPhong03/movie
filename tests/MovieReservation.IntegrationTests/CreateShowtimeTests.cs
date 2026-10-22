using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Features.Showtimes.Create;
using MovieReservation.Domain.Entities;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Kiểm tra 2 luật chính khi tạo suất chiếu trên Postgres thật (Testcontainers):
/// sinh đủ ShowtimeSeat cho mọi ghế của phòng, và từ chối suất trùng giờ cùng phòng.
/// Gọi handler trực tiếp, mỗi giai đoạn Arrange / Act / Assert dùng một DbContext mới.
/// </summary>
public class CreateShowtimeTests : IAsyncLifetime
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

    // Phim 120 phút + phòng 2 hàng x 3 ghế = 6 ghế
    private async Task<(int MovieId, int TheaterId)> SeedAsync()
    {
        await using var db = new AppDbContext(_options);

        var movie = new Movie { Title = "Phim test", DurationMinutes = 120, Genre = "Action" };
        var theater = new Theater { Name = "Phòng test" };
        theater.GenerateSeats(rows: 2, seatsPerRow: 3);

        db.Movies.Add(movie);
        db.Theaters.Add(theater);
        await db.SaveChangesAsync();

        return (movie.Id, theater.Id);
    }

    [Fact]
    public async Task Tao_suat_chieu_thi_sinh_ShowtimeSeat_bang_so_ghe_cua_phong()
    {
        // Arrange
        var (movieId, theaterId) = await SeedAsync();
        var startAt = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc);

        // Act
        int showtimeId;
        await using (var db = new AppDbContext(_options))
        {
            var handler = new CreateShowtimeCommandHandler(db);
            var result = await handler.Handle(
                new CreateShowtimeCommand(movieId, startAt, theaterId, 90000), CancellationToken.None);
            showtimeId = result.Id;
        }

        // Assert: đọc lại từ DB bằng context mới, không đọc từ bộ nhớ của context ở bước Act
        await using (var db = new AppDbContext(_options))
        {
            var seatCount = await db.ShowtimeSeats.CountAsync(s => s.ShowtimeId == showtimeId);
            Assert.Equal(6, seatCount);
        }
    }

    [Fact]
    public async Task Tao_suat_chong_gio_cung_phong_thi_bi_tu_choi()
    {
        // Arrange: suất 1 chiếu 18:00 → 20:15 (120 phút + 15 phút dọn phòng)
        var (movieId, theaterId) = await SeedAsync();
        var firstStart = new DateTime(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc);
        await using (var db = new AppDbContext(_options))
        {
            await new CreateShowtimeCommandHandler(db).Handle(
                new CreateShowtimeCommand(movieId, firstStart, theaterId, 90000), CancellationToken.None);
        }

        // Act + Assert: suất 2 bắt đầu 19:00, nằm giữa suất 1 → phải ném ConflictException
        await using (var db = new AppDbContext(_options))
        {
            var handler = new CreateShowtimeCommandHandler(db);
            var overlapping = new CreateShowtimeCommand(movieId, firstStart.AddHours(1), theaterId, 90000);

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(overlapping, CancellationToken.None));
        }
    }
}
