using AnnouncementBot.Application.Commands.Subscriptions;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class SubscribeCallbackHandler : ICallbackHandler
{
    private readonly IMediator _mediator;

    public SubscribeCallbackHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public bool CanHandle(string callbackData) => callbackData.StartsWith("subscribe:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var categoryId = Guid.Parse(callbackQuery.Data!.Replace("subscribe:", ""));

        await _mediator.Send(new ToggleSubscriptionCommand(userId, categoryId), ct);

        await bot.SendMessage(
            callbackQuery.Message!.Chat.Id,
            "✅ Подписка обновлена.",
            cancellationToken: ct);
    }
}