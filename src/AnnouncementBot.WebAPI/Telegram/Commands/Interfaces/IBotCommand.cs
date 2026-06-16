using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.WebApi.Telegram.Commands.Interfaces
{
    public interface IBotCommand
    {
        string Command { get; }
        Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct);
    }
}
