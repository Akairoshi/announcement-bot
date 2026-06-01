using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;

public class AnnouncementCategoryState : IConversationState
{
    private readonly long _userId;
    public readonly string Text;
    public readonly Guid? TemplateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AnnouncementCategoryState(long userId, string text, Guid? templateId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        Text = text;
        TemplateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        // ждём callback — если пришло сообщение напоминаем
        await bot.SendMessage(
            message.Chat.Id,
            "⚠️ Выберите категорию из списка выше.",
            cancellationToken: ct);
    }
}