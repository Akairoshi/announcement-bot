using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
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

        var (text, keyboard) = user?.Role switch
        {
            UserRole.SuperAdmin => (
                "👑 Добро пожаловать, Супер Администратор!",
                new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("/profile"), new KeyboardButton("/list_announcement") },
                    new[] { new KeyboardButton("/make_announcement") },
                    new[] { new KeyboardButton("/list_category"), new KeyboardButton("/add_category") },
                    new[] { new KeyboardButton("/update_category"), new KeyboardButton("/remove_category") },
                    new[] { new KeyboardButton("/list_admin"), new KeyboardButton("/remove_admin") },
                    new[] { new KeyboardButton("/list_admin_request") },
                    new[] { new KeyboardButton("/list_template"), new KeyboardButton("/add_template") },
                    new[] { new KeyboardButton("/update_template"), new KeyboardButton("/remove_template") }
                })
                { ResizeKeyboard = true }),

            UserRole.Admin => (
                "🔧 Добро пожаловать, Администратор!",
                new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("/profile"), new KeyboardButton("/list_announcement") },
                    new[] { new KeyboardButton("/make_announcement") },
                    new[] { new KeyboardButton("/list_template"), new KeyboardButton("/add_template") },
                    new[] { new KeyboardButton("/update_template"), new KeyboardButton("/remove_template") }
                })
                { ResizeKeyboard = true }),

            _ => (
                "👋 Добро пожаловать в AnnouncementBot!\n\nЯ рассылаю уведомления по категориям.",
                new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("/profile"), new KeyboardButton("/subscribe") },
                    new[] { new KeyboardButton("/list_announcement") },
                    new[] { new KeyboardButton("/admin_request") }
                })
                { ResizeKeyboard = true })
        };

        await bot.SendMessage(
            message.Chat.Id,
            text,
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}