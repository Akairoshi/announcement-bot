using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class CancelCommand : IBotCommand
{
    public string Command => "/cancel";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        // Так как UpdateHandler уже сам очистил стейдж, здесь мы занимаемся только UI
        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "❌ Действие отменено. Вы вернулись в главное меню.",
            cancellationToken: ct);
    }
}