using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.WebApi.Telegram.FSM;

public interface IConversationState
{
    Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct);
}