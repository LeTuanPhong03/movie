using Microsoft.EntityFrameworkCore;
using MovieReservation.Domain.Entities;
namespace MovieReservation.Infrastructure.Persistence;

public class AppDbContext : DbContext
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


}