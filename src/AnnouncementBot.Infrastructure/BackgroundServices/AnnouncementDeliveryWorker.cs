using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
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

        var categoryIds = announcementsCache.Values.Select(a => a.CategoryId).Distinct().ToHashSet();
        var allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        var categoriesCache = allCategories
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        foreach (var delivery in pendingDeliveries)
        {
            if (!announcementsCache.TryGetValue(delivery.AnnouncementId, out var announcement))
            {
                _logger.LogWarning("Объявление {Id} не найдено, пропускаем.", delivery.AnnouncementId);
                delivery.MarkAsFailed(DeliveryErrorStatus.NotFound);
                continue;
            }

            var categoryName = categoriesCache.TryGetValue(announcement.CategoryId, out var category)
                ? category.Name
                : "Без категории";

            try
            {
                await _botClient.SendMessage(
                    chatId: delivery.UserId,
                    text: $"📢 <b>{categoryName}</b>\n\n{announcement.Text}",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);

                delivery.MarkAsSent();
            }
            catch (ApiRequestException apiEx)
            {
                var errorStatus = apiEx.ErrorCode switch
                {
                    400 => DeliveryErrorStatus.BadRequest,
                    401 => DeliveryErrorStatus.Unauthorized,
                    403 => DeliveryErrorStatus.Forbidden,
                    404 => DeliveryErrorStatus.NotFound,
                    429 => DeliveryErrorStatus.TooManyRequests,
                    500 => DeliveryErrorStatus.InternalServerError,
                    _ => DeliveryErrorStatus.BadRequest
                };

                _logger.LogWarning(
                    "Telegram ошибка [{Code}] для пользователя {UserId}: {Message}",
                    apiEx.ErrorCode, delivery.UserId, apiEx.Message);

                delivery.MarkAsFailed(errorStatus);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogWarning(
                    "Сетевая ошибка для пользователя {UserId}: {Message}",
                    delivery.UserId, httpEx.Message);

                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Таймаут при отправке пользователю {UserId}.", delivery.UserId);
                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
            }

            await Task.Delay(50, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }
}
