using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MovieReservation.Application.Common.Behaviors;
using MovieReservation.Application.Features.Auth;

namespace MovieReservation.Application;

/// <summary>
/// Đăng ký dịch vụ của tầng Application vào DI: MediatR (quét handler), ValidationBehavior,
/// mọi validator, và AuthTokenIssuer (cấp token dùng chung cho Register/Login/Refresh).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthTokenIssuer>();
        return services;
    }
}
