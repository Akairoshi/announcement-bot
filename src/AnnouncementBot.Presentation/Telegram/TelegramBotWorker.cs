using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.Configuration;
using AnnouncementBot.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram;

public class TelegramBotWorker : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUpdateHandler _updateHandler;
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLifetime;

    public TelegramBotWorker(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ILogger<TelegramBotWorker> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime appLifetime)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _appLifetime = appLifetime;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        bool isDbReady = false;
        bool isTelegramReady = false;

        var delayInterval = TimeSpan.FromSeconds(10);

        _logger.LogInformation("Запуск проверки и подготовки необходимых ресурсов...");

        while (!cancellationToken.IsCancellationRequested)
        {
            if (!isDbReady)
            {
                _logger.LogInformation("Проверка подключения к базе данных...");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                try
                {
                    if (await dbContext.Database.CanConnectAsync(cancellationToken))
                    {
                        _logger.LogInformation("Подключение к базе данных установлено.");

                        await EnsureSuperAdminAsync(scope, cancellationToken);
                        isDbReady = true;
                    }
                    else
                    {
                        _logger.LogError("База данных недоступна на порту. Проверьте строку подключения или запуск контейнера.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Ошибка при работе с базе данных: {Message}", ex.Message);
                }
            }

            if (isDbReady && !isTelegramReady)
            {
                _logger.LogInformation("Проверка соединения с Telegram API...");
                try
                {
                    var me = await _botClient.GetMe(cancellationToken);
                    _logger.LogInformation("Соединение с Telegram установлено! Бот: @{Username}", me.Username);
                    isTelegramReady = true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Нет связи с Telegram API: {Message}", ex.Message);
                }
            }

            if (isDbReady && isTelegramReady)
            {
                _logger.LogInformation("Все ресурсы успешно подготовлены. Бот готов к запуску.");
                break;
            }

            _logger.LogInformation("Ожидание ресурсов... Следующая проверка через 10 секунд.");

            try
            {
                await Task.Delay(delayInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Проверка ресурсов была прервана отменой приложения.");
                _appLifetime.StopApplication();
                return;
            }
        }

        await base.StartAsync(cancellationToken);
    }

    private async Task EnsureSuperAdminAsync(IServiceScope scope, CancellationToken ct)
    {
        var superAdminConfig = scope.ServiceProvider
            .GetRequiredService<IOptions<SuperAdminConfiguration>>()
            .Value;

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByIdAsync(superAdminConfig.UserId, ct);

        if (user is null)
        {
            var superAdmin = new User(superAdminConfig.UserId, null);
            superAdmin.ChangeRole(UserRole.SuperAdmin);
            await unitOfWork.Users.AddAsync(superAdmin, ct);
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("SuperAdmin создан: {Id}", superAdminConfig.UserId);
        }
        else if (user.Role != UserRole.SuperAdmin)
        {
            user.ChangeRole(UserRole.SuperAdmin);
            await unitOfWork.Users.UpdateAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("SuperAdmin роль восстановлена: {Id}", superAdminConfig.UserId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery },
            DropPendingUpdates = true
        };

        _logger.LogInformation("Бот запущен и слушает обновления...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _botClient.ReceiveAsync(
                    updateHandler: _updateHandler,
                    receiverOptions: receiverOptions,
                    cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка polling — переподключение через 5 секунд...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}