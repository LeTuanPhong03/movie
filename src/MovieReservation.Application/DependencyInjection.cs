using Microsoft.Extensions.DependencyInjection;

namespace MovieReservation.Application;
/// <summary>
/// Đăng ký các dịch vụ của tầng Application vào DI: MediatR.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        return services;
    }
}
