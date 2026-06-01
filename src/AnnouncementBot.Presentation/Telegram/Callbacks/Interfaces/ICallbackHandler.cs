using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;

public interface ICallbackHandler
{
    bool CanHandle(string callbackData);
    Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct);
}