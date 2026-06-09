using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.Keyboards;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class StartCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public string Command => "/start";

    public StartCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);

        ReplyKeyboardMarkup mainMenuKeyboard = user.Role switch
        {
            UserRole.SuperAdmin => ReplyKeyboards.GetSuperAdminMainMenu(),
            UserRole.Admin => ReplyKeyboards.GetAdminMainMenu(),
            _ => ReplyKeyboards.GetUserMainMenu()
        };
        var welcomeText = "👋 Добро пожаловать в Бот объявлений!\n";

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: welcomeText,
            replyMarkup: mainMenuKeyboard,
            cancellationToken: ct);
    }
}