using System.Text.RegularExpressions;
using AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateFillState : IConversationState
{
    private readonly long _userId;
    private readonly string _templateText;
    private readonly List<string> _placeholders;
    private readonly Dictionary<string, string> _values;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateFillState(
        long userId,
        string templateText,
        Guid? templateId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _templateText = templateText;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
        _values = new Dictionary<string, string>();

        _placeholders = Regex.Matches(templateText, @"\{(\w+)\}")
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
    }

    private string? CurrentPlaceholder => _placeholders
        .FirstOrDefault(p => !_values.ContainsKey(p));

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "⚠️ Значение не может быть пустым. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }

        if (CurrentPlaceholder is not null)
            _values[CurrentPlaceholder] = input;

        var next = CurrentPlaceholder;

        if (next is not null)
        {
            await bot.SendMessage(
                message.Chat.Id,
                $"✏️ Введите значение для {{{next}}}:",
                cancellationToken: ct);
            return;
        }

        var finalText = _values.Aggregate(
            _templateText,
            (text, kv) => text.Replace($"{{{kv.Key}}}", kv.Value));

        _stateStorage.Set(_userId, new AnnouncementTextState(
            _userId, null, _scopeFactory, _stateStorage));

        _stateStorage.Set(_userId, new AnnouncementCategoryState(
            _userId, finalText, null, _scopeFactory, _stateStorage));

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider
            .GetRequiredService<AnnouncementBot.Domain.Interfaces.IUnitOfWork>();

        var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(_userId, ct);
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var access in accesses)
        {
            var category = await unitOfWork.Categories.GetByIdAsync(access.CategoryId, ct);
            if (category is not null)
                buttons.Add(new[]
                {
                    InlineKeyboardButton
                        .WithCallbackData(category.Name, $"announcement_category:{category.Id}")
                });
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"📋 Предпросмотр:\n\n{finalText}\n\n📂 Выберите категорию:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}