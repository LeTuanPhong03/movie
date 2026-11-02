using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Infrastructure.Persistence.Configurations;

/// <summary>
/// Cấu hình bảng Payments: TxnRef unique (IPN tìm thanh toán theo mã này, trùng là tìm nhầm đơn),
/// Status lưu dạng chữ, tiền cùng độ chính xác với Reservation.TotalPrice, không xoá đơn khi còn thanh toán.
/// </summary>
public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.TxnRef).HasMaxLength(50);
        builder.HasIndex(p => p.TxnRef).IsUnique();
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(p => p.Amount).HasPrecision(10, 2);
        builder.Property(p => p.ProviderTransactionNo).HasMaxLength(50);
        builder.Property(p => p.ResponseCode).HasMaxLength(10);
        builder.HasOne<Reservation>()
            .WithMany()
            .HasForeignKey(p => p.ReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
