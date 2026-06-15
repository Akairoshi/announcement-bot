using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnnouncementBot.Infrastructure.BackgroundServices;

public class AnnouncementCleanerWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AnnouncementCleanerWorker> _logger;
    private readonly TimeSpan _interval;
    private const int RetentionDays = 30;

    public AnnouncementCleanerWorker(
        IServiceProvider serviceProvider,
        ILogger<AnnouncementCleanerWorker> logger,
        IOptions<BotConfiguration> botOptions)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var minutes = int.TryParse(botOptions.Value.SenderInterval, out var interval) ? interval : 3;
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[СИСТЕМА] Воркер очистки объявлений запущен. Интервал: {Minutes} мин.", _interval.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanOldAnnouncementsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ОШИБКА] Сбой в цикле очистки объявлений.");
            }

            await Task.Delay(_interval, stoppingToken);
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
            _logger.LogInformation("[СИСТЕМА] Очистка объявлений | Устаревших записей не найдено.");
            return;
        }

        await unitOfWork.Announcements.DeleteRangeAsync(old, ct);
        await unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("[СИСТЕМА] Очистка объявлений | Удалено {Count} объявлений старше {Days} дней.", old.Count, RetentionDays);
    }
}