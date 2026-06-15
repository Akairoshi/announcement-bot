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
    private bool _wasNetworkDown = false;

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
                _logger.LogError("Ошибка в цикле рассылки: {Message}", ex.Message);
            }

            _logger.LogInformation("Следующий запуск рассылки через {Minutes} мин.", Interval.TotalMinutes);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDeliveryQueueAsync(CancellationToken ct)
    {
        _logger.LogInformation("Воркер рассылки запущен в {Time}.", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingDeliveries = await unitOfWork.DeliveryStatuses.GetPendingOrFailedAsync(MaxRetryCount, ct);

        if (!pendingDeliveries.Any())
        {
            _logger.LogInformation("Нет доставок для обработки.");
            return;
        }

        _logger.LogInformation("Обработка {Count} доставок.", pendingDeliveries.Count);

        var announcementIds = pendingDeliveries.Select(d => d.AnnouncementId).Distinct().ToList();
        var allAnnouncements = await unitOfWork.Announcements.GetAllAsync(ct);
        var announcementsCache = allAnnouncements
            .Where(a => announcementIds.Contains(a.Id))
            .ToDictionary(a => a.Id);

        var categoryIds = announcementsCache.Values
            .Where(a => a.CategoryId.HasValue)
            .Select(a => a.CategoryId!.Value)
            .Distinct()
            .ToHashSet();
        var allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        var categoriesCache = allCategories
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionary(c => c.Id);

        bool hadNetworkErrorInBatch = false;

        foreach (var delivery in pendingDeliveries)
        {
            if (!announcementsCache.TryGetValue(delivery.AnnouncementId, out var announcement))
            {
                _logger.LogWarning("Объявление {Id} не найдено, пропускаем.", delivery.AnnouncementId);
                delivery.MarkAsFailed(DeliveryErrorStatus.NotFound);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
                continue;
            }

            var categoryName = announcement.CategoryId.HasValue && categoriesCache.TryGetValue(announcement.CategoryId.Value, out var category)
                ? category.Name
                : "Без категории";

            try
            {
                await _botClient.SendMessage(
                    chatId: delivery.UserId,
                    text: $"📢 <b>{categoryName}</b>\n\n{announcement.Text}",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);

                if (_wasNetworkDown)
                {
                    _logger.LogInformation("Сетевое соединение восстановлено. Рассылка продолжается.");
                    _wasNetworkDown = false;
                }

                delivery.MarkAsSent();
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
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
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }
            catch (RequestException requestEx)
            {
                _logger.LogWarning("Сетевая ошибка для пользователя {UserId}: {Message}",
                    delivery.UserId, requestEx.Message);

                hadNetworkErrorInBatch = true;
                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Таймаут при отправке пользователю {UserId}.", delivery.UserId);

                hadNetworkErrorInBatch = true;
                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }

            await Task.Delay(50, ct);
        }

        if (hadNetworkErrorInBatch)
            _wasNetworkDown = true;

        await unitOfWork.SaveChangesAsync(ct);
    }
}
