using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.WebApi.Telegram.Keyboards;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class CancelCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;
    public string Command => "/cancel";

    public CancelCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);

        ReplyKeyboardMarkup mainMenuKeyboard = ReplyKeyboards.GetMainKeyboard(user!.Role);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "❌ Действие отменено. Вы вернулись в главное меню.",
            replyMarkup: mainMenuKeyboard,
            cancellationToken: ct);
    }
}