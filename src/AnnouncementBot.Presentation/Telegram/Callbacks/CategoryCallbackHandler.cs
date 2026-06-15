using AnnouncementBot.Application.Commands.Categories;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class CategoryCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public CategoryCallbackHandler(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData) =>
        callbackData.StartsWith("cat_upd:") ||
        callbackData.StartsWith("cat_del_sel:") ||
        callbackData.StartsWith("cat_del_yes:") ||
        callbackData == "cat_del_no";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data!;
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        if (data.StartsWith("cat_upd:"))
        {
            var categoryId = Guid.Parse(data["cat_upd:".Length..]);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            _stateStorage.Set(userId, new UpdateCategoryState(categoryId, userId, _stateStorage, _scopeFactory));

            await bot.SendMessage(
                chatId,
                "📂 <b>Редактирование категории</b>\n\nВведите новое название категории:\n\nДля отмены введите /cancel",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("cat_del_sel:"))
        {
            var categoryId = Guid.Parse(data["cat_del_sel:".Length..]);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var category = await unitOfWork.Categories.GetByIdAsync(categoryId, ct);
            if (category is null)
            {
                await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
                await bot.SendMessage(chatId, "❌ Категория не найдена.", cancellationToken: ct);
                return;
            }

            var subscribers = await unitOfWork.Subscriptions.GetByCategoryIdAsync(categoryId, ct);
            var subscriberCount = subscribers.Count;

            var subscriberLine = subscriberCount > 0
                ? $"\n👥 Подписчиков: {subscriberCount} — они получат уведомление об автоматической отписке."
                : "\n👥 Подписчики отсутствуют.";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"cat_del_yes:{categoryId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "cat_del_no")
                }
            });

            await bot.EditMessageText(
                chatId,
                messageId,
                $"🗑 Удалить категорию <b>\"{category.Name}\"</b>?{subscriberLine}\n\n⚠️ Объявления этой категории потеряют к ней привязку.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("cat_del_yes:"))
        {
            var categoryId = Guid.Parse(data["cat_del_yes:".Length..]);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var category = await unitOfWork.Categories.GetByIdAsync(categoryId, ct);
            if (category is null)
            {
                await bot.SendMessage(chatId, "❌ Категория не найдена.", cancellationToken: ct);
                return;
            }

            var categoryName = category.Name;
            var subscribers = await unitOfWork.Subscriptions.GetByCategoryIdAsync(categoryId, ct);
            var subscriberIds = subscribers.Select(s => s.UserId).ToList();

            try
            {
                await mediator.Send(new RemoveCategoryCommand(categoryId, userId), ct);

                await bot.SendMessage(
                    chatId,
                    $"✅ Категория <b>\"{categoryName}\"</b> удалена. Уведомлено подписчиков: {subscriberIds.Count}.",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);

                foreach (var subscriberId in subscriberIds)
                {
                    try
                    {
                        await bot.SendMessage(
                            chatId: subscriberId,
                            text: $"ℹ️ Категория <b>\"{categoryName}\"</b> была удалена. Вы автоматически отписаны.",
                            parseMode: ParseMode.Html,
                            cancellationToken: ct);
                    }
                    catch
                    {
                        // Игнорируем ошибки доставки (например, бот заблокирован)
                    }
                }
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
            }

            return;
        }

        if (data == "cat_del_no")
        {
            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
            await bot.SendMessage(chatId, "❌ Удаление отменено.", cancellationToken: ct);
        }
    }
}