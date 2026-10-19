using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Infrastructure.Persistence;

/// <summary>
/// Tạo tài khoản Admin lúc app khởi động (Admin không đăng ký qua API được).
/// Email lấy từ appsettings (Admin:Email), mật khẩu từ user-secrets / biến môi trường (Admin:Password),
/// không bao giờ nằm trong code. Idempotent: admin đã tồn tại thì bỏ qua.
/// </summary>
public class AdminSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<AdminSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var email = configuration["Admin:Email"]?.Trim().ToLowerInvariant();
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Chưa cấu hình Admin:Email / Admin:Password → bỏ qua seed admin.");
            return;
        }

        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return;
        }

        db.Users.Add(new User
        {
            FullName = "Administrator",
            Email = email,
            PasswordHash = passwordHasher.HashPassword(password),
            Role = UserRole.Admin
        });
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Đã tạo tài khoản admin {Email}.", email);
    }
}
