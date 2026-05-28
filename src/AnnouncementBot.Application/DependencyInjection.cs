using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using AnnouncementBot.Application.Common.Behaviors;

namespace AnnouncementBot.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(assembly);
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(AuditLogBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}