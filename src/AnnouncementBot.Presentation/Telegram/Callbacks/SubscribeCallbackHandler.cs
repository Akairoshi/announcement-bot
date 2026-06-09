using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Application.Commands.Subscriptions;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

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
            await bot.AnswerCallbackQuery(callbackQuery.Id, "❌ Ошибка данных", cancellationToken: ct);
            return;
        }

        var userId = callbackQuery.From.Id;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Переключаем подписку в БД через твой MediatR-хендлер
        await mediator.Send(new ToggleSubscriptionCommand(userId, categoryId), ct);

        // Гасим часики загрузки на кнопке
        await bot.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);

        // Пересобираем клавиатуру с обновленным статусом для этого сообщения
        var categories = await unitOfWork.Categories.GetAllAsync(ct);
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var c in categories)
        {
            var sub = await unitOfWork.Subscriptions.GetByUserAndCategoryAsync(userId, c.Id, ct);
            var isSubscribed = sub is not null;
            var buttonText = isSubscribed ? $"✅ {c.Name}" : $"🔔 {c.Name}";

            buttons.Add(new[] { InlineKeyboardButton.WithCallbackData(buttonText, $"subscribe:{c.Id}") });
        }

        // Обновляем кнопки в текущем сообщении (переключаем галочку на лету)
        await bot.EditMessageReplyMarkup(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}