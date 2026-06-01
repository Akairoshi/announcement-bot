using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
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
        => callbackData.StartsWith("announcement_template:") ||
           callbackData.StartsWith("announcement_category:") ||
           callbackData.StartsWith("announcement_confirm:") ||
           callbackData == "announcement_cancel";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        if (data.StartsWith("announcement_template:"))
            await HandleTemplateSelectedAsync(bot, userId, chatId, data, ct);

        else if (data.StartsWith("announcement_category:"))
            await HandleCategorySelectedAsync(bot, userId, chatId, data, ct);

        else if (data.StartsWith("announcement_confirm:"))
            await HandleConfirmAsync(bot, userId, chatId, ct);

        else if (data == "announcement_cancel")
        {
            _stateStorage.Clear(userId);
            await bot.SendMessage(chatId, "❌ Отменено.", cancellationToken: ct);
        }
    }

    private async Task HandleTemplateSelectedAsync(ITelegramBotClient bot, long userId, long chatId, string data, CancellationToken ct)
    {
        var templatePart = data.Replace("announcement_template:", "");

        if (templatePart == "none")
        {
            _stateStorage.Set(userId, new AnnouncementTextState(userId, null, _scopeFactory, _stateStorage));
            await bot.SendMessage(
                chatId,
                "📝 Введите текст объявления:",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
            return;
        }

        var templateId = Guid.Parse(templatePart);
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
            var fillState = new TemplateFillState(userId, template.Text, templateId, _scopeFactory, _stateStorage);
            _stateStorage.Set(userId, fillState);

            var firstPlaceholder = Regex.Match(template.Text, @"\{(\w+)\}").Groups[1].Value;
            await bot.SendMessage(
                chatId,
                $"✏️ Введите значение для {{{firstPlaceholder}}}:",
                replyMarkup: new ReplyKeyboardRemove(), // ← добавь
                cancellationToken: ct);
        }
        else
        {
            _stateStorage.Set(userId, new AnnouncementCategoryState(userId, template.Text, templateId, _scopeFactory, _stateStorage));
            await ShowCategoriesAsync(bot, userId, chatId, ct);
        }
    }

    private async Task HandleCategorySelectedAsync(ITelegramBotClient bot, long userId, long chatId, string data, CancellationToken ct)
    {
        var categoryId = Guid.Parse(data.Replace("announcement_category:", ""));
        var state = _stateStorage.Get(userId) as AnnouncementCategoryState;

        if (state is null)
        {
            await bot.SendMessage(chatId, "❌ Сессия устарела. Начните заново.", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(userId, new AnnouncementConfirmState(
            userId, state.Text, categoryId, state.TemplateId, _scopeFactory, _stateStorage));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Отправить", "announcement_confirm:yes"),
                InlineKeyboardButton.WithCallbackData("❌ Отменить", "announcement_cancel")
            }
        });

        await bot.SendMessage(
            chatId,
            $"📋 Предпросмотр:\n\n{state.Text}\n\nОтправить?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private async Task HandleConfirmAsync(ITelegramBotClient bot, long userId, long chatId, CancellationToken ct)
    {
        var state = _stateStorage.Get(userId) as AnnouncementConfirmState;
        if (state is not null)
            await state.ConfirmAsync(bot, chatId, ct);
    }

    private async Task ShowCategoriesAsync(ITelegramBotClient bot, long userId, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var access in accesses)
        {
            var category = await unitOfWork.Categories.GetByIdAsync(access.CategoryId, ct);
            if (category is not null)
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(category.Name, $"announcement_category:{category.Id}")
                });
        }

        await bot.SendMessage(
            chatId,
            "📂 Выберите категорию:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}