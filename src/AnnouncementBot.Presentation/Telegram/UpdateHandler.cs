using AnnouncementBot.Presentation.Middlewares;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram;

public class UpdateHandler : IUpdateHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ConversationStateStorage _stateStorage;

    public UpdateHandler(
        IServiceProvider serviceProvider,
        ILogger<UpdateHandler> logger,
        ConversationStateStorage stateStorage)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _stateStorage = stateStorage;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var commands = scope.ServiceProvider.GetServices<IBotCommand>();
        var middlewares = scope.ServiceProvider.GetServices<IBotMiddleware>();

        await ExecuteMiddlewarePipelineAsync(update, middlewares,
            () => RouteAsync(botClient, update, commands, ct), ct);
    }

    private async Task ExecuteMiddlewarePipelineAsync(
        Update update,
        IEnumerable<IBotMiddleware> middlewares,
        Func<Task> final,
        CancellationToken ct)
    {
        var pipeline = middlewares.Reverse().Aggregate(final,
            (next, middleware) => () => middleware.InvokeAsync(update, next, ct));

        await pipeline();
    }

    private async Task RouteAsync(
        ITelegramBotClient botClient,
        Update update,
        IEnumerable<IBotCommand> commands,
        CancellationToken ct)
    {
        if (update.Message?.Text is { } text)
        {
            await HandleMessageAsync(botClient, update.Message, commands, text.Trim(), ct);
        }
        else if (update.CallbackQuery is { } callbackQuery)
        {
            await HandleCallbackQueryAsync(botClient, callbackQuery, ct);
        }
        else
        {
            _logger.LogWarning("Неизвестный тип события: {UpdateType}", update.Type);
        }
    }

    private async Task HandleMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        IEnumerable<IBotCommand> commands,
        string text,
        CancellationToken ct)
    {
        var userId = message.From!.Id;
        _logger.LogInformation("[СООБЩЕНИЕ] От ID {UserId}: {Text}", userId, text);

        if (text == "/cancel" || text.StartsWith("/cancel "))
        {
            var cancelCommand = commands.FirstOrDefault(c => c.Command == "/cancel");
            if (cancelCommand is not null)
            {
                await cancelCommand.ExecuteAsync(botClient, message, ct);
                return;
            }
        }


        var activeState = _stateStorage.Get(userId);
        if (activeState is not null)
        {
            await activeState.HandleAsync(botClient, message, ct);
            return;
        }

        var command = commands.FirstOrDefault(c => text == c.Command)
            ?? commands.FirstOrDefault(c => text.StartsWith(c.Command + " "));

        if (command is not null)
        {
            await command.ExecuteAsync(botClient, message, ct);
        }
        else
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Неизвестная команда. Используй /start для начала.",
                cancellationToken: ct);
        }
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken ct)
    {
        var data = callbackQuery.Data ?? string.Empty;
        _logger.LogInformation("[КНОПКА] Юзер {UserId} нажал: {Data}", callbackQuery.From.Id, data);

        await botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

        using var scope = _serviceProvider.CreateScope();
        var callbackHandlers = scope.ServiceProvider.GetServices<ICallbackHandler>();
        var handler = callbackHandlers.FirstOrDefault(h => h.CanHandle(data));

        if (handler is not null)
            await handler.HandleAsync(botClient, callbackQuery, ct);
        else
            _logger.LogWarning("Нет обработчика для callback: {Data}", data);
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Ошибка Long Polling");
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        _logger.LogError(exception, "Ошибка Telegram");
        return Task.CompletedTask;
    }
}