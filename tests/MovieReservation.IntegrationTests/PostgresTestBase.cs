using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using MovieReservation.Application.Features.Reservations.Cancel;
using MovieReservation.Application.Features.Reservations.Confirm;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Domain.Entities;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Nền chung cho các test gọi thẳng handler trên Postgres thật (Testcontainers): mỗi class test một container,
/// seed 2 user + 2 phim + 1 phòng 2x3 ghế + 2 suất (2030-01-01 18:00 và 2030-01-02 18:00 UTC), và các hàm
/// đặt / xác nhận / huỷ đơn. Mỗi lần gọi handler dùng một DbContext mới, giống một request thật.
/// </summary>
public abstract class PostgresTestBase : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    protected DbContextOptions<AppDbContext> Options = null!;

    protected static readonly DateTime Show1StartUtc = new(2030, 1, 1, 18, 0, 0, DateTimeKind.Utc);
    protected static readonly DateTime Show2StartUtc = new(2030, 1, 2, 18, 0, 0, DateTimeKind.Utc);
    protected const decimal Price = 90000;

    protected record Seeded(int UserA, int UserB, int Show1, int Show2, List<int> Show1Seats, List<int> Show2Seats);

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        Options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await using var db = NewDb();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    protected AppDbContext NewDb() => new(Options);

    // Đồng hồ 17:00 ngày 1/1/2030, 1 tiếng trước suất 1
    protected static FakeTimeProvider Clock() => new(new DateTime(2030, 1, 1, 17, 0, 0, DateTimeKind.Utc));

    protected async Task<Seeded> SeedAsync()
    {
        await using var db = NewDb();
        var movie1 = new Movie { Title = "Phim A", DurationMinutes = 120, Genre = "Action" };
        var movie2 = new Movie { Title = "Phim B", DurationMinutes = 90, Genre = "Drama" };
        var theater = new Theater { Name = "Phòng test" };
        theater.GenerateSeats(rows: 2, seatsPerRow: 3);
        var userA = new User { FullName = "A", Email = "a@example.com", PasswordHash = "x" };
        var userB = new User { FullName = "B", Email = "b@example.com", PasswordHash = "x" };
        var show1 = new Showtime { Movie = movie1, Theater = theater, StartAt = Show1StartUtc, EndAt = Show1StartUtc.AddMinutes(135), Price = Price };
        var show2 = new Showtime { Movie = movie2, Theater = theater, StartAt = Show2StartUtc, EndAt = Show2StartUtc.AddMinutes(105), Price = Price };
        db.AddRange(movie1, movie2, theater, userA, userB, show1, show2);
        await db.SaveChangesAsync();
        show1.GenerateShowtimeSeats(theater.Seats);
        show2.GenerateShowtimeSeats(theater.Seats);
        await db.SaveChangesAsync();
        return new Seeded(userA.Id, userB.Id, show1.Id, show2.Id,
            show1.ShowtimeSeats.Select(s => s.Id).ToList(), show2.ShowtimeSeats.Select(s => s.Id).ToList());
    }

    protected async Task<int> DatAsync(TimeProvider clock, int userId, int showtimeId, List<int> seatIds)
    {
        await using var db = NewDb();
        var don = await new CreateReservationCommandHandler(db, clock)
            .Handle(new CreateReservationCommand(userId, showtimeId, seatIds), CancellationToken.None);
        return don.Id;
    }

    protected async Task XacNhanAsync(TimeProvider clock, int reservationId, int userId)
    {
        await using var db = NewDb();
        await new ConfirmReservationCommandHandler(db, clock)
            .Handle(new ConfirmReservationCommand(reservationId, userId), CancellationToken.None);
    }

    protected async Task HuyAsync(TimeProvider clock, int reservationId, int userId, bool isAdmin = false)
    {
        await using var db = NewDb();
        await new CancelReservationCommandHandler(db, clock)
            .Handle(new CancelReservationCommand(reservationId, userId, isAdmin), CancellationToken.None);
    }
}
