using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MovieReservation.Application.Common.Behaviors;

namespace MovieReservation.Application;
/// <summary>
/// Extension method để đăng ký các dịch vụ của tầng Application vào DI.
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
        return services;
    }
}
