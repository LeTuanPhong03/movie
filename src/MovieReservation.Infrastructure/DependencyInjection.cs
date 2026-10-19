using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Infrastructure.Identity;
using MovieReservation.Infrastructure.Persistence;

namespace MovieReservation.Infrastructure;
/// <summary>
/// Đăng ký các dịch vụ của tầng Infrastructure vào DI: DbContext (và IAppDbContext trỏ về cùng
/// một DbContext mỗi request), hash mật khẩu BCrypt, JWT, TimeProvider. Program.cs chỉ cần gọi AddInfrastructure.
/// </summary>

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext: chuyển dòng AddDbContext vào đây, để Program.cs chỉ cần gọi AddInfrastructure
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("Default")));
        // Lấy đúng DbContext của request này, không tạo cái thứ 2
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        // IPasswordHasher → BCryptPasswordHasher
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        // ITokenService → JwtTokenService
        services.AddScoped<ITokenService, JwtTokenService>();
        // TimeProvider
        services.AddSingleton(TimeProvider.System);
        // Seed admin lúc khởi động (Program.cs gọi)
        services.AddScoped<AdminSeeder>();
        return services;
    }
}
