using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class MakeAnnouncementCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public MakeAnnouncementCallbackHandler(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData) =>
        callbackData.StartsWith("ann_cat:") ||
        callbackData.StartsWith("ann_tpl:") ||
        callbackData.StartsWith("ann_confirm:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data!;
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (data.StartsWith("ann_cat:"))
        {
            var categoryId = Guid.Parse(data.Split(':')[1]);
            _stateStorage.Set(userId, new AnnouncementCategorySelectedState(categoryId));

            var templates = await unitOfWork.Templates.GetByAdminIdAsync(userId, ct);

            var buttons = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("➕ Без шаблона", "ann_tpl:none") }
            };

            foreach (var t in templates)
                buttons.Add(new[] { InlineKeyboardButton.WithCallbackData($"📄 {t.Name}", $"ann_tpl:{t.Id}") });

            await bot.EditMessageText(
                chatId: chatId,
                messageId: messageId,
                text: "📢 <b>Создание объявления</b>\n\nВыберите шаблон или продолжите без него:\n\nДля отмены введите /cancel",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("ann_tpl:"))
        {
            if (_stateStorage.Get(userId) is not AnnouncementCategorySelectedState categoryState)
            {
                await bot.SendMessage(chatId, "❌ Сессия истекла. Начните создание заново через /make_announcement.", cancellationToken: ct);
                return;
            }

            var categoryId = categoryState.CategoryId;
            var rawTemplateId = data.Split(':')[1];

            Guid? templateId = null;
            string templateText;

            if (rawTemplateId == "none")
            {
                templateText = "{Текст объявления}";
            }
            else
            {
                templateId = Guid.Parse(rawTemplateId);
                var template = await unitOfWork.Templates.GetByIdAsync(templateId.Value, ct);
                if (template is null)
                {
                    await bot.SendMessage(chatId, "❌ Шаблон не найден.", cancellationToken: ct);
                    return;
                }
                templateText = template.Text;
            }

            var fillState = new FillTemplateState(categoryId, templateId, templateText, _scopeFactory);
            _stateStorage.Set(userId, fillState);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
            await bot.SendMessage(
                chatId: chatId,
                text: $"📝 <b>Текст шаблона:</b>\n{templateText}",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            await fillState.AdvanceDialogueAsync(bot, chatId, userId, ct);

            return;
        }

        if (data.StartsWith("ann_confirm:"))
        {
            if (_stateStorage.Get(userId) is not FillTemplateState activeState)
            {
                await bot.SendMessage(chatId, "⚠️ Сессия устарела или сброшена.", cancellationToken: ct);
                return;
            }

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            var decision = data.Split(':')[1];
            await activeState.HandleConfirmationAsync(bot, decision, userId, ct);

            _stateStorage.Clear(userId);
        }
    }
}