using System.Text.RegularExpressions;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AnnouncementCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AnnouncementCallbackHandler(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("ann_cat:") ||
           callbackData.StartsWith("ann_tpl:") ||
           callbackData.StartsWith("ann_cnf:") ||
           callbackData == "ann_cancel";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        if (data.StartsWith("ann_cat:"))
            await HandleCategorySelectedAsync(bot, userId, chatId, callbackQuery.Message.MessageId, data, ct);

        else if (data.StartsWith("ann_tpl:"))
            await HandleTemplateSelectedAsync(bot, userId, chatId, callbackQuery.Message.MessageId, data, ct);

        else if (data.StartsWith("ann_cnf:"))
            await HandleConfirmAsync(bot, userId, chatId, callbackQuery.Message.MessageId, ct);

        else if (data == "ann_cancel")
            await HandleCancelAsync(bot, userId, chatId, callbackQuery.Message.MessageId, ct);
    }

    // ШАГ 1 — выбрана категория → показываем шаблоны
    private async Task HandleCategorySelectedAsync(
        ITelegramBotClient bot,
        long userId,
        long chatId,
        int messageId,
        string data,
        CancellationToken ct)
    {
        var categoryId = Guid.Parse(data.Replace("ann_cat:", ""));

        _stateStorage.Set(userId, new AnnouncementCategorySelectedState(
            userId, categoryId, _scopeFactory, _stateStorage));

        await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var templates = await unitOfWork.Templates.GetByAdminIdAsync(userId, ct);

        var buttons = templates
            .Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.Name, $"ann_tpl:{t.Id}") })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("✏️ Без шаблона", "ann_tpl:none") });

        await bot.SendMessage(
            chatId,
            "📋 Выберите шаблон или продолжите без него:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }

    // ШАГ 2 — выбран шаблон → текст или заполнение плейсхолдеров
    private async Task HandleTemplateSelectedAsync(
        ITelegramBotClient bot,
        long userId,
        long chatId,
        int messageId,
        string data,
        CancellationToken ct)
    {
        var state = _stateStorage.Get(userId) as AnnouncementCategorySelectedState;

        if (state is null)
        {
            await bot.SendMessage(chatId, "⚠️ Сессия устарела. Начните заново через /make_announcement.", cancellationToken: ct);
            return;
        }

        var categoryId = state.CategoryId;
        var tplPart = data.Replace("ann_tpl:", "");

        await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

        if (tplPart == "none")
        {
            // без шаблона — ждём текст
            _stateStorage.Set(userId, new AnnouncementTextState(
                userId, categoryId, null, _scopeFactory, _stateStorage));

            await bot.SendMessage(
                chatId,
                "📝 Введите текст объявления:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
            return;
        }

        if (!Guid.TryParse(tplPart, out var templateId))
        {
            await bot.SendMessage(chatId, "❌ Неверный шаблон.", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var template = await unitOfWork.Templates.GetByIdAsync(templateId, ct);

        if (template is null)
        {
            await bot.SendMessage(chatId, "❌ Шаблон не найден.", cancellationToken: ct);
            return;
        }

        var hasPlaceholders = Regex.IsMatch(template.Text, @"\{(\w+)\}");

        if (hasPlaceholders)
        {
            var fillState = new TemplateFillState(
                userId, template.Text, categoryId, templateId, _scopeFactory, _stateStorage);

            _stateStorage.Set(userId, fillState);
            await fillState.StartAsync(bot, chatId, ct);
        }
        else
        {
            // шаблон без плейсхолдеров — сразу предпросмотр
            await ShowConfirmationAsync(bot, userId, chatId, template.Text, categoryId, templateId, ct);
        }
    }

    // ШАГ 3 — подтверждение отправки
    private async Task HandleConfirmAsync(
        ITelegramBotClient bot,
        long userId,
        long chatId,
        int messageId,
        CancellationToken ct)
    {
        await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

        var state = _stateStorage.Get(userId) as AnnouncementConfirmState;

        if (state is not null)
            await state.ConfirmAsync(bot, chatId, ct);
        else
            await bot.SendMessage(chatId, "⚠️ Сессия устарела. Начните заново.", cancellationToken: ct);
    }

    private async Task HandleCancelAsync(
        ITelegramBotClient bot,
        long userId,
        long chatId,
        int messageId,
        CancellationToken ct)
    {
        _stateStorage.Clear(userId);
        await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
        await bot.SendMessage(chatId, "❌ Создание объявления отменено.", cancellationToken: ct);
    }

    // вспомогательный метод — показ предпросмотра и кнопок подтверждения
    public async Task ShowConfirmationAsync(
        ITelegramBotClient bot,
        long userId,
        long chatId,
        string text,
        Guid categoryId,
        Guid? templateId,
        CancellationToken ct)
    {
        _stateStorage.Set(userId, new AnnouncementConfirmState(
            userId, text, categoryId, templateId, _scopeFactory, _stateStorage));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🚀 Отправить", "ann_cnf:yes") },
            new[] { InlineKeyboardButton.WithCallbackData("❌ Отмена", "ann_cancel") }
        });

        await bot.SendMessage(
            chatId,
            $"📋 Предпросмотр:\n\n{text}\n\nОтправить?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}
