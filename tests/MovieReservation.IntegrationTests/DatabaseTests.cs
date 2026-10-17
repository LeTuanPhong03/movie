using Microsoft.EntityFrameworkCore;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace MovieReservation.IntegrationTests;

public class DatabaseTests : IAsyncLifetime
{
    // "Bản thiết kế" container: Postgres 17, giống docker-compose
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migration_chay_xong_thi_du_bang_va_khong_con_migration_cho()
    {
        // Arrange: tạo DbContext trỏ vào Postgres của container
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var db = new AppDbContext(options);

        // Act: chạy migration
        await db.Database.MigrateAsync();

        // Assert:
        // TODO 1: không còn migration nào chưa áp
        var pendingMigrations = await db.Database.GetPendingMigrationsAsync();
        Assert.Empty(pendingMigrations);
        // TODO 2: bảng ShowtimeSeats tồn tại và đang trống
        var showtimeSeatsCount = await db.ShowtimeSeats.CountAsync();
        Assert.Equal(0, showtimeSeatsCount);
    }
}
