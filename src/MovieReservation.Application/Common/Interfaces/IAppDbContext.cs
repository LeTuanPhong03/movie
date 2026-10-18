using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;
namespace MovieReservation.Application.Common.Interfaces;


/// <summary>
/// Interface cho DbContext của ứng dụng.
/// </summary>
/// <remarks>
/// Cổng truy cập DB cho tầng Application: các bảng (DbSet) và lưu thay đổi.
/// Cài đặt bởi AppDbContext ở Infrastructure. Dùng thay repository (xem docs/decisions.md mục 3).
/// </remarks>

public interface IAppDbContext
{
    DbSet<Movie> Movies { get; }
    DbSet<Reservation> Reservations { get; }
    DbSet<Seat> Seats { get; }
    DbSet<Theater> Theaters { get; }
    DbSet<Showtime> Showtimes { get; }
    DbSet<User> Users { get; }
    DbSet<ShowtimeSeat> ShowtimeSeats { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}