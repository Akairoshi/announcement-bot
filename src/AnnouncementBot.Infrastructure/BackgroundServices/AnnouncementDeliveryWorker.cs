using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Infrastructure.BackgroundServices;

public class AnnouncementDeliveryWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<AnnouncementDeliveryWorker> _logger;
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

    public AnnouncementDeliveryWorker(
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient,
        ILogger<AnnouncementDeliveryWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _botClient = botClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Воркер рассылки объявлений запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDeliveryQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле рассылки.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDeliveryQueueAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingDeliveries = await unitOfWork.DeliveryStatuses.GetPendingOrFailedAsync(MaxRetryCount, ct);

        if (!pendingDeliveries.Any())
            return;

        _logger.LogInformation("Обработка {Count} доставок.", pendingDeliveries.Count);

        var announcementIds = pendingDeliveries.Select(d => d.AnnouncementId).Distinct().ToList();
        var allAnnouncements = await unitOfWork.Announcements.GetAllAsync(ct);
        var announcementsCache = allAnnouncements
            .Where(a => announcementIds.Contains(a.Id))
            .ToDictionary(a => a.Id);

        foreach (var delivery in pendingDeliveries)
        {
            if (!announcementsCache.TryGetValue(delivery.AnnouncementId, out var announcement))
            {
                _logger.LogWarning("Объявление {Id} не найдено, пропускаем.", delivery.AnnouncementId);
                delivery.MarkAsFailed();
                continue;
            }

            try
            {
                var category = unitOfWork.Categories.GetByIdAsync(announcement.CategoryId, ct).Result;
                await _botClient.SendMessage(
                    chatId: delivery.UserId,
                    text: $"📢 <b>Объявление - {category?.Name}</b>\n\n{announcement.Text}",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);

                delivery.MarkAsSent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка отправки объявления {AnnId} пользователю {UserId}.",
                    delivery.AnnouncementId, delivery.UserId);

                delivery.MarkAsFailed();
            }

            await Task.Delay(50, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
