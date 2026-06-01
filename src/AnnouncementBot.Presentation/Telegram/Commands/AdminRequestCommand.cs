using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Category;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class AdminRequestCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public string Command => "/admin_request";

    public AdminRequestCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);

        if (user is null) return;

        if (user.Role == UserRole.Admin)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🔄 Переназначить роль", "admin_request:reassignment") }
            });

            await bot.SendMessage(
                message.Chat.Id,
                "Выберите тип заявки:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
        else
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📋 Стать администратором", "admin_request:appointment") }
            });

            await bot.SendMessage(
                message.Chat.Id,
                "Выберите тип заявки:",
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}