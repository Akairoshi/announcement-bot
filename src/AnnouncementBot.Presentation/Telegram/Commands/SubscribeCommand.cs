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
            await bot.SendMessage(message.Chat.Id, "📭 Список категорий пуст.", cancellationToken: ct);
            return;
        }

        // Рендерим кнопки с динамическими галочками
        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var c in categories)
        {
            // Проверяем, подписан ли юзер на эту конкретную категорию
            var sub = await unitOfWork.Subscriptions.GetByUserAndCategoryAsync(userId, c.Id, ct);
            var isSubscribed = sub is not null;

            // Если подписан — добавляем галочку, если нет — просто колокольчик
            var buttonText = isSubscribed ? $"✅ {c.Name}" : $"🔔 {c.Name}";

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"subscribe:{c.Id}") });
        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "📋 Нажмите на категорию, чтобы подписаться или отписаться:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}