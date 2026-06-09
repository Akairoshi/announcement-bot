using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AnnouncementBot.Infrastructure.BackgroundServices;

public class AnnouncementCleanerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnnouncementCleanerWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int RetentionDays = 30;

    public AnnouncementCleanerWorker(
        IServiceProvider serviceProvider,
        ILogger<AnnouncementCleanerWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Воркер очистки объявлений запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanOldAnnouncementsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле очистки объявлений.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CleanOldAnnouncementsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var threshold = DateTime.UtcNow.AddDays(-RetentionDays);
        var old = await unitOfWork.Announcements.GetOlderThanAsync(threshold, ct);

        if (!old.Any())
        {
            _logger.LogInformation("Устаревших объявлений не найдено.");
            return;
        }

        await unitOfWork.Announcements.DeleteRangeAsync(old, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Удалено {Count} объявлений старше {Days} дней.", old.Count, RetentionDays);
    }
}
