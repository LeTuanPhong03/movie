using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Infrastructure.Persistence;

/// <summary>
/// Dữ liệu demo cho bản deploy, chỉ chạy khi Seed:Demo = true: 3 phim, 2 phòng, 1 tài khoản khách demo,
/// và suất chiếu cho DaysAhead ngày tới. Chạy mỗi lần khởi động nhưng idempotent: phim/phòng/tài khoản có rồi
/// thì bỏ qua, ngày nào phòng đã có suất thì không thêm → app ngủ dậy (Render free) là tự có suất mới.
/// Giờ chiếu viết theo giờ Việt Nam rồi đổi sang UTC trước khi lưu (PLAN quy tắc 5).
/// </summary>
public class DemoSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<DemoSeeder> logger)
{
    private const int DaysAhead = 7;
    // Cách nhau 3,5 tiếng > phim dài nhất (150') + dọn phòng (15') → không bao giờ trùng giờ trong một phòng
    private static readonly TimeOnly[] SlotTimes = [new(9, 30), new(13, 0), new(16, 30), new(20, 0)];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Seed:Demo"))
            return;

        await SeedDemoUserAsync(cancellationToken);

        if (!await db.Movies.AnyAsync(cancellationToken))
        {
            db.Movies.AddRange(
                new Movie { Title = "Lật Mặt 8", Genre = "Hành động", DurationMinutes = 125, SuitableAge = 16,
                            Description = "Phim hành động Việt Nam (dữ liệu demo)." },
                new Movie { Title = "Hành Tinh Cát", Genre = "Khoa học viễn tưởng", DurationMinutes = 150, SuitableAge = 13,
                            Description = "Phim viễn tưởng (dữ liệu demo)." },
                new Movie { Title = "Doraemon: Bản Giao Hưởng", Genre = "Hoạt hình", DurationMinutes = 100, SuitableAge = 0,
                            Description = "Phim hoạt hình (dữ liệu demo)." });
            var room1 = new Theater { Name = "Phòng 1" };
            room1.GenerateSeats(rows: 8, seatsPerRow: 10);
            var room2 = new Theater { Name = "Phòng 2" };
            room2.GenerateSeats(rows: 6, seatsPerRow: 8);
            db.Theaters.AddRange(room1, room2);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Đã tạo phim và phòng demo.");
        }

        await SeedShowtimesAsync(cancellationToken);
    }

    private async Task SeedDemoUserAsync(CancellationToken cancellationToken)
    {
        var email = configuration["Seed:DemoUserEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Seed:DemoUserPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)
            || await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            return;

        db.Users.Add(new User
        {
            FullName = "Khách Demo",
            Email = email,
            PasswordHash = passwordHasher.HashPassword(password),
            Role = UserRole.Customer
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedShowtimesAsync(CancellationToken cancellationToken)
    {
        var vn = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(nowUtc, vn));

        var movies = await db.Movies.Where(m => m.IsActive).OrderBy(m => m.Id).Take(3).ToListAsync(cancellationToken);
        var theaters = await db.Theaters.Include(t => t.Seats).OrderBy(t => t.Id).Take(2).ToListAsync(cancellationToken);
        if (movies.Count == 0 || theaters.Count == 0)
            return;

        int created = 0;
        for (int d = 0; d < DaysAhead; d++)
        {
            var day = today.AddDays(d);
            var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), vn);
            var dayEndUtc = dayStartUtc.AddDays(1);

            for (int t = 0; t < theaters.Count; t++)
            {
                var theater = theaters[t];
                if (await db.Showtimes.AnyAsync(s => s.TheaterId == theater.Id && s.StartAt >= dayStartUtc && s.StartAt < dayEndUtc, cancellationToken))
                    continue;

                for (int slot = 0; slot < SlotTimes.Length; slot++)
                {
                    var startUtc = TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(SlotTimes[slot]), vn);
                    if (startUtc <= nowUtc)
                        continue;
                    // Xoay vòng phim theo ngày + phòng + suất để mỗi phim đều có suất ở cả 2 phòng
                    var movie = movies[(d + t + slot) % movies.Count];
                    var showtime = new Showtime
                    {
                        MovieId = movie.Id,
                        TheaterId = theater.Id,
                        StartAt = startUtc,
                        EndAt = startUtc.AddMinutes(movie.DurationMinutes + Showtime.CleanupMinutes),
                        Price = slot >= 2 ? 110_000 : 85_000,
                    };
                    showtime.GenerateShowtimeSeats(theater.Seats);
                    db.Showtimes.Add(showtime);
                    created++;
                }
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        if (created > 0)
            logger.LogInformation("Đã tạo {Count} suất chiếu demo.", created);
    }
}
