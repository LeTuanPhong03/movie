using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        builder.Property(m => m.Title)
            .HasMaxLength(200);
        builder.Property(m => m.Description)
            .HasMaxLength(2000);
        builder.Property(m => m.Genre)
            .HasMaxLength(50);
        builder.Property(m => m.PosterUrl)
            .HasMaxLength(500);
    }
}
