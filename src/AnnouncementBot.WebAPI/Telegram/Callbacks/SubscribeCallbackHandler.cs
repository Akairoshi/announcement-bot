using AnnouncementBot.WebApi.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Application.Commands.Subscriptions;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Callbacks;

public class SubscribeCallbackHandler : ICallbackHandler
{
    private readonly IServiceProvider _serviceProvider;

    public SubscribeCallbackHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool CanHandle(string callbackData) => callbackData.StartsWith("subscribe:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var parts = callbackQuery.Data!.Split(':');
        if (parts.Length < 2 || !Guid.TryParse(parts[1], out var categoryId))
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, "❌ Данные некорректны.", cancellationToken: ct);
            return;
        }

        var userId = callbackQuery.From.Id;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await mediator.Send(new ToggleSubscriptionCommand(userId, categoryId), ct);

        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

        var categories = await unitOfWork.Categories.GetAllAsync(ct);
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var c in categories)
        {
            var sub = await unitOfWork.Subscriptions.GetByUserAndCategoryAsync(userId, c.Id, ct);
            var isSubscribed = sub is not null;
            var buttonText = isSubscribed ? $"🔔 {c.Name}" : $"{c.Name}";

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"subscribe:{c.Id}") });
        }

        await bot.EditMessageReplyMarkup(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}