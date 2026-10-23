using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

public class ShowtimeSeatConfiguration : IEntityTypeConfiguration<ShowtimeSeat>
{
    public void Configure(EntityTypeBuilder<ShowtimeSeat> builder)
    {
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.HasOne<Seat>(s => s.Seat)
            .WithMany()
            .HasForeignKey(s => s.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Showtime>()
            .WithMany(st => st.ShowtimeSeats)
            .HasForeignKey(st => st.ShowtimeId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.ShowtimeId, s.SeatId }).IsUnique();
        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(s => s.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
