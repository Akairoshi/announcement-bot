// Presentation/Telegram/Commands/SubscribeCommand.cs
using AnnouncementBot.Application.Commands.Subscriptions;
using AnnouncementBot.Application.Queries.Categories;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class SubscribeCommand : IBotCommand
{
    private readonly IMediator _mediator;

    public string Command => "/subscribe";

    public SubscribeCommand(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        var categories = await _mediator.Send(
            new GetCategoriesWithSubscriptionQuery(userId), ct);

        if (!categories.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📋 Категорий пока нет.",
                cancellationToken: ct);
            return;
        }

        // строим инлайн кнопки — каждая категория отдельная кнопка
        var buttons = categories.Select(c =>
        {
            var label = c.IsSubscribed
                ? $"✅ {c.Name}"
                : $"➕ {c.Name}";

            return new[] { InlineKeyboardButton.WithCallbackData(label, $"subscribe:{c.Id}") };
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await bot.SendMessage(
            message.Chat.Id,
            "📋 Выберите категории для подписки/отписки:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}