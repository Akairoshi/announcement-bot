using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.BackgroundServices;
using AnnouncementBot.Infrastructure.Configuration;
using AnnouncementBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnnouncementBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BotConfiguration>(
            configuration.GetSection(BotConfiguration.SectionName));

        services.Configure<SuperAdminConfiguration>(
            configuration.GetSection(SuperAdminConfiguration.SectionName));

        var connectionSettings = configuration
            .GetSection(ConnectionSettings.SectionName)
            .Get<ConnectionSettings>() ?? throw new InvalidOperationException("Connection settings are not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionSettings.ToConnectionString()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHostedService<AnnouncementDeliveryWorker>();
        services.AddHostedService<AnnouncementCleanerWorker>();

        return services;
    }
}
