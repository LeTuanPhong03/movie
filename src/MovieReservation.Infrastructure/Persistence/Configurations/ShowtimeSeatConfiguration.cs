using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình bảng ShowtimeSeats: Status lưu dạng chuỗi, khóa ngoại tới Seat/Showtime/Reservation,
/// mỗi ghế chỉ xuất hiện 1 lần trong 1 suất (unique ShowtimeId + SeatId),
/// và RowVersion ánh xạ sang cột hệ thống xmin để chống ghi đè khi 2 người cùng sửa 1 ghế.
/// </summary>
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
        builder.HasOne<Reservation>(s => s.Reservation)
            .WithMany()
            .HasForeignKey(s => s.ReservationId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.Property(s => s.RowVersion)
            .IsRowVersion();
    }
}
