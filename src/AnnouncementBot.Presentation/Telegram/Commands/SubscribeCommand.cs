using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class SubscribeCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public SubscribeCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/subscribe";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var categories = await unitOfWork.Categories.GetAllAsync(ct);
        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Категории отсутствуют.", cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var c in categories)
        {
            var sub = await unitOfWork.Subscriptions.GetByUserAndCategoryAsync(userId, c.Id, ct);
            var isSubscribed = sub is not null;

            var buttonText = isSubscribed ? $"✅ {c.Name}" : $"🔔 {c.Name}";

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"subscribe:{c.Id}") });
        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "📋 Выберите категорию для управления подпиской:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}