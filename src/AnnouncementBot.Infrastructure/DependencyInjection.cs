using AnnouncementBot.Infrastructure.Configuration;
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
            .Get<ConnectionSettings>();

        //services.AddDbContext<AppDbContext>(options =>
        //    options.UseNpgsql(connectionSettings.ToConnectionString()));

        return services;
    }
}