using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM;

public interface IConversationState
{
    Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct);
}