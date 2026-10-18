using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;
using MovieReservation.Application.Common.Interfaces;
namespace MovieReservation.Infrastructure.Persistence;


/// <summary>
/// Lớp DbContext để thao tác với cơ sở dữ liệu.
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Movie> Movies { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<Theater> Theaters { get; set; }
    public DbSet<Showtime> Showtimes { get; set; }
    public DbSet<Seat> Seats { get; set; }
    public DbSet<ShowtimeSeat> ShowtimeSeats { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}