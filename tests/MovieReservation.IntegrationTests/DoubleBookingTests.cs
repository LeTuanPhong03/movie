using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;
using MovieReservation.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using MovieReservation.Application.Features.Reservations.Create;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Domain.Enums;
namespace MovieReservation.IntegrationTests;

/// <summary>
///Kiểm tra chống đặt trùng ghế trên Postgres thật: nhiều người tranh 1 ghế, và 2 người đặt 2 tập ghế giao nhau (người thua không giữ ghế nào). Mỗi task 1 DbContext riêng để giống request thật.
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
    private async Task<(int UserId, int ShowtimeId, List<int> ShowtimeSeatIds)> SeedAsync()
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
        return (user.Id, showtime.Id, showtime.ShowtimeSeats.Select(ss => ss.Id).ToList());
    }
    private async Task<Exception?> DatGheAsync(int userId, int showtimeId, List<int> showtimeSeatIds)
    {
        await using var db = new AppDbContext(_options);
        var handler = new CreateReservationCommandHandler(db, TimeProvider.System);
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
    public async Task Dat_1_ghe_trong_thi_tao_1_don()
    {
        // Arrange
        var (userId, showtimeId, showtimeSeatIds) = await SeedAsync();

        // Act
        await using (var db = new AppDbContext(_options))
        {
            var handler = new CreateReservationCommandHandler(db,TimeProvider.System);
            await handler.Handle(
                new CreateReservationCommand(userId, showtimeId, [showtimeSeatIds[0]]), CancellationToken.None);
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
        const int soNguoi = 20; 
        var (userId, showtimeId, showtimeSeatIds) = await SeedAsync();
    
        // Act
        var tasks = Enumerable.Range(0, soNguoi).Select(_ => DatGheAsync(userId, showtimeId, [showtimeSeatIds[0]])).ToArray();
        var results = await Task.WhenAll(tasks);
        
        // Assert
        var soNguoiThanhCong = results.Count(r => r == null);
        var soNguoiNhanConflict = results.Count(r => r is ConflictException);
        await using (var db = new AppDbContext(_options))
        {
            var soDon = await db.Reservations.CountAsync();
            Assert.Equal(1, soNguoiThanhCong);
            Assert.Equal(1, soDon);
            Assert.All(results.Where(r => r != null), r => Assert.IsType<ConflictException>(r));
            Assert.Equal(soNguoi - 1, soNguoiNhanConflict);
        
        }
    }
    [Fact]
    public async Task Hai_nguoi_dat_ghe_giao_nhau_thi_nguoi_thua_khong_giu_ghe_nao()
    {
        // Arrange
        var (userId, showtimeId, showtimeSeatIds) = await SeedAsync();
        var ghe1 =  showtimeSeatIds[0]; // ghế đầu tiên
        var ghe2 =  showtimeSeatIds[1]; // ghế thứ hai
        var ghe3 =  showtimeSeatIds[2]; // ghế thứ ba
        // Act
        var tasks = new[]
        {
            DatGheAsync(userId, showtimeId, new List<int> { ghe1, ghe2 }), // người 1 đặt ghế 1 và 2
            DatGheAsync(userId, showtimeId, new List<int> { ghe2, ghe3 })// người 2 đặt ghế 2 và 3
        };
        var results = await Task.WhenAll(tasks);
        // Assert
        Assert.Equal(1, results.Count(r => r == null)); // chỉ 1 người thành công
        Assert.All(results.Where(r => r != null), r => Assert.IsType<ConflictException>(r)); // người còn lại thua đúng lý do
        var gheNguoiThang = results[0] == null ? new List<int> { ghe1, ghe2 } : new List<int> { ghe2, ghe3 };
        var gheChiNguoiThuaMuon = results[0] == null ? ghe3 : ghe1;
        await using (var db = new AppDbContext(_options))
        {
            var don = await db.Reservations.SingleAsync();
            var cacGheNguoiThang = await db.ShowtimeSeats
                .Where(s => gheNguoiThang.Contains(s.Id))   // lọc: chỉ lấy ghế của người thắng
                .ToListAsync();

            Assert.Equal(2, cacGheNguoiThang.Count);          // lấy ra đủ 2 ghế
            Assert.All(cacGheNguoiThang, ghe =>
            {
                Assert.Equal(SeatStatus.Held, ghe.Status);
                Assert.Equal(don.Id, ghe.ReservationId);
            });
            var gheCuaNguoiThua  = await db.ShowtimeSeats.SingleAsync(s => s.Id == gheChiNguoiThuaMuon);
            Assert.Equal(SeatStatus.Available, gheCuaNguoiThua.Status);
            Assert.Null(gheCuaNguoiThua.ReservationId);
        }
    }
}