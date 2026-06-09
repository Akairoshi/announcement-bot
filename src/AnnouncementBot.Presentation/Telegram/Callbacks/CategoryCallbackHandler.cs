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
                "✏️ Введите новое название категории:\n\n<i>Для отмены введите /cancel</i>",
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
                $"🗑 Удалить категорию <b>\"{category.Name}\"</b>?\n\n⚠️ Все объявления в этой категории останутся, но потеряют привязку.",
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
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new RemoveCategoryCommand(categoryId, userId), ct);
                await bot.SendMessage(chatId, "✅ Категория удалена.", cancellationToken: ct);
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
