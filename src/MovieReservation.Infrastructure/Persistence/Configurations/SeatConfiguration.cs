using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.Property(s => s.Row)
            .HasMaxLength(5);
        builder.HasOne<Theater>()
            .WithMany()
            .HasForeignKey(s => s.TheaterId)
            .OnDelete(DeleteBehavior.Cascade);
        // Trong 1 phòng không có 2 ghế cùng hàng, cùng số (vd: 2 ghế "A5")
        builder.HasIndex(s => new { s.TheaterId, s.Row, s.Number }).IsUnique();
    }
}
