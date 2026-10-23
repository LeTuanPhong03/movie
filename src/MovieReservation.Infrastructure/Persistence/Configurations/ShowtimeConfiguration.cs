using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

public class ShowtimeConfiguration : IEntityTypeConfiguration<Showtime>
{
    public void Configure(EntityTypeBuilder<Showtime> builder)
    {
        builder.Property(s => s.Price)
            .HasPrecision(10, 2);
        builder.HasOne<Movie>(s => s.Movie)
            .WithMany()
            .HasForeignKey(s => s.MovieId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Theater>(s => s.Theater)
            .WithMany()
            .HasForeignKey(s => s.TheaterId)
            .OnDelete(DeleteBehavior.Restrict);
        // Tăng tốc kiểm tra trùng giờ: tìm các suất của 1 phòng theo giờ bắt đầu
        builder.HasIndex(s => new { s.TheaterId, s.StartAt });
    }
}
