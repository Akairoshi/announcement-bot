using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.Configuration;
using AnnouncementBot.Infrastructure.Persistence; // Для AppDbContext
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

    public TelegramBotWorker(
        ITelegramBotClient botClient,
        IUpdateHandler updateHandler,
        ILogger<TelegramBotWorker> logger,
        IServiceProvider serviceProvider)
    {
        _botClient = botClient;
        _updateHandler = updateHandler;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Проверка подключения к базе данных...");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            if (await dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Подключение к PostgreSQL установлено!");
                await EnsureSuperAdminAsync(scope, cancellationToken);
            }
            else
            {
                _logger.LogError("База данных не найдена. Накати миграции.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Не удалось подключиться к PostgreSQL!");
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