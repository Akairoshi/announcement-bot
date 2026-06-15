using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly TimeSpan Interval;
    private bool _wasNetworkDown = false;

    public AnnouncementDeliveryWorker(
        IServiceProvider serviceProvider,
        ITelegramBotClient botClient,
        ILogger<AnnouncementDeliveryWorker> logger,
        IOptions<BotConfiguration> botOptions)
    {
        _serviceProvider = serviceProvider;
        _botClient = botClient;
        _logger = logger;

        var minutes = int.TryParse(botOptions.Value.SenderInterval, out var interval) ? interval : 3;
        Interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[СИСТЕМА] Воркер рассылки объявлений запущен.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDeliveryQueueAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError("[ОШИБКА] Критический сбой в цикле рассылки | {Message}", ex.Message);
            }

            _logger.LogInformation("[СИСТЕМА] Ожидание следующего цикла | Интервал: {Minutes} мин [{Time}].", Interval.TotalMinutes, (DateTime.UtcNow + Interval));
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ProcessDeliveryQueueAsync(CancellationToken ct)
    {
        _logger.LogInformation("[СИСТЕМА] Запуск обработки очереди рассылки | Время: {Time}", DateTime.UtcNow);

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendingDeliveries = await unitOfWork.DeliveryStatuses.GetPendingOrFailedAsync(MaxRetryCount, ct);

        if (!pendingDeliveries.Any())
        {
            _logger.LogInformation("[СИСТЕМА] Очередь рассылки пуста | Нет доставок для обработки.");
            return;
        }

        _logger.LogInformation("[СИСТЕМА] Найдено записей для отправки | Количество: {Count}", pendingDeliveries.Count);

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
                _logger.LogWarning("[ПРЕДУПРЕЖДЕНИЕ] Объявление не найдено | ID: {Id} | Доставка отменена.", delivery.AnnouncementId);
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
                    _logger.LogInformation("[СИСТЕМА] Сетевое соединение восстановлено | Рассылка возобновлена.");
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
                    "[ПРЕДУПРЕЖДЕНИЕ] API ошибка Telegram | Код: {Code} | Пользователь: {UserId} | Текст: {Message}",
                    apiEx.ErrorCode, delivery.UserId, apiEx.Message);

                delivery.MarkAsFailed(errorStatus);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }
            catch (RequestException requestEx)
            {
                _logger.LogError("[СБОЙ СОЕДИНЕНИЯ] Ошибка сети при отправке | Пользователь: {UserId} | Текст: {Message}",
                    delivery.UserId, requestEx.Message);

                hadNetworkErrorInBatch = true;
                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("[СБОЙ СОЕДИНЕНИЯ] Таймаут операции SendMessage | Пользователь: {UserId}", delivery.UserId);

                hadNetworkErrorInBatch = true;
                delivery.MarkAsFailed(DeliveryErrorStatus.NetworkError);
                await unitOfWork.DeliveryStatuses.UpdateAsync(delivery, ct);
            }

            await Task.Delay(50, ct);
        }

        if (hadNetworkErrorInBatch)
            _wasNetworkDown = true;

        await unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("[СИСТЕМА] Обработка пачки завершена | Изменения сохранены в БД.");
    }
}