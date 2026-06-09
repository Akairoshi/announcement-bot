using System.Text.RegularExpressions;
using AnnouncementBot.Application.Commands.Announcements;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class FillTemplateState : IConversationState
{
    private static readonly Regex PlaceholderRegex = new(@"\{([^}]+)\}", RegexOptions.Compiled);

    private readonly Guid _categoryId;
    private readonly Guid? _templateId;
    private readonly string _templateText;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly List<string> _remainingPlaceholders;
    private readonly Dictionary<string, string> _filledValues = new();
    private bool _isWaitingForConfirmation = false;

    public FillTemplateState(Guid categoryId, Guid? templateId, string templateText, IServiceScopeFactory scopeFactory)
    {
        _categoryId = categoryId;
        _templateId = templateId;
        _templateText = templateText;
        _scopeFactory = scopeFactory;

        _remainingPlaceholders = PlaceholderRegex.Matches(templateText)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var text = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text) || text.StartsWith('/'))
        {
            await bot.SendMessage(chatId, "⚠️ Пожалуйста, введите корректное текстовое значение (команды не принимаются):", cancellationToken: ct);
            return;
        }

        if (_isWaitingForConfirmation)
        {
            await bot.SendMessage(chatId, "⚠️ Пожалуйста, используйте инлайн-кнопки под сообщением для подтверждения или отмены.", cancellationToken: ct);
            return;
        }

        var currentPlaceholder = _remainingPlaceholders[0];
        _filledValues[currentPlaceholder] = text;
        _remainingPlaceholders.RemoveAt(0);
        await AdvanceDialogueAsync(bot, chatId, message.From!.Id, ct);
    }

    public async Task AdvanceDialogueAsync(ITelegramBotClient bot, long chatId, long userId, CancellationToken ct)
    {
        if (_remainingPlaceholders.Count > 0)
        {
            var nextPlaceholder = _remainingPlaceholders[0];
            await bot.SendMessage(
                chatId: chatId,
                text: $"📝 Введите значение для переменной: <b>{{{nextPlaceholder}}}</b>\n\n" +
            "<i>Для отмены введите /cancel</i>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        _isWaitingForConfirmation = true;
        string finalizedText = BuildFinalText();

        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Отправить", "ann_confirm:yes"),
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "ann_confirm:no")
            }
        });

        await bot.SendMessage(
            chatId: chatId,
            text: $"🔍 <b>Предпросмотр объявления:</b>\n\n{finalizedText}\n\nВы подтверждаете отправку?",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }

    public async Task HandleConfirmationAsync(ITelegramBotClient bot, string decision, long userId, CancellationToken ct)
    {
        if (decision == "yes")
        {
            string finalizedText = BuildFinalText();

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateAnnouncementCommand(
                Text: finalizedText,
                CategoryId: _categoryId,
                CreatedById: userId,
                TemplateId: _templateId
            );

            var announcementId = await mediator.Send(command, ct);

            await bot.SendMessage(
                chatId: userId,
                text: $"🚀 <b>Объявление успешно создано и отправлено в очередь доставки!</b>\nID: <code>{announcementId}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        else
        {
            await bot.SendMessage(userId, "❌ Создание объявления отменена.", cancellationToken: ct);
        }
    }

    private string BuildFinalText()
    {
        string result = _templateText;
        foreach (var pair in _filledValues)
        {
            result = result.Replace($"{{{pair.Key}}}", pair.Value);
        }
        return result;
    }
}