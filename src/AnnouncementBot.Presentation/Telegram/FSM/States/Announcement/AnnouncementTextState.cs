using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using AnnouncementBot.Domain.Interfaces;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;

public class AnnouncementTextState : IConversationState
{
    private readonly long _userId;
    private readonly Guid? _templateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AnnouncementTextState(long userId, Guid? templateId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _templateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var text = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Текст не может быть пустым:", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(_userId, new AnnouncementCategoryState(
            _userId, text, _templateId, _scopeFactory, _stateStorage));

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(_userId, ct);

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

        if (!buttons.Any())
        {
            await bot.SendMessage(message.Chat.Id, "❌ У вас нет доступных категорий.", cancellationToken: ct);
            _stateStorage.Clear(_userId);
            return;
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"📋 Предпросмотр:\n\n{text}\n\n📂 Выберите категорию:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}