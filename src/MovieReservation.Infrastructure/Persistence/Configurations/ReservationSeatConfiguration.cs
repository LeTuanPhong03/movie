using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình bảng ReservationSeats: thuộc về Reservation (cha → con qua Reservation.Seats),
/// trỏ tới ghế vật lý Seat (Restrict: không xoá ghế đã từng bán), mỗi ghế chỉ xuất hiện 1 lần trong 1 đơn.
/// </summary>
public class ReservationSeatConfiguration : IEntityTypeConfiguration<ReservationSeat>
{
    public void Configure(EntityTypeBuilder<ReservationSeat> builder)
    {
        builder.HasOne<Reservation>()
            .WithMany(r => r.Seats)
            .HasForeignKey(rs => rs.ReservationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rs => rs.Seat)
            .WithMany()
            .HasForeignKey(rs => rs.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rs => new { rs.ReservationId, rs.SeatId }).IsUnique();
    }
}
