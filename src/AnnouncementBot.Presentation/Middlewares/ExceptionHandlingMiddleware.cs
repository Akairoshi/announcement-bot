using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Middlewares;

public class ExceptionHandlingMiddleware : IBotMiddleware
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ITelegramBotClient bot, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _bot = bot;
        _logger = logger;
    }

    public async Task InvokeAsync(Update update, Func<Task> next, CancellationToken ct)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке Update {UpdateId}", update.Id);

            var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;

            if (chatId is not null)
            {
                await _bot.SendMessage(
                    chatId,
                    "⚠️ Произошла ошибка. Попробуйте позже.",
                    cancellationToken: ct);
            }
        }
    }
}