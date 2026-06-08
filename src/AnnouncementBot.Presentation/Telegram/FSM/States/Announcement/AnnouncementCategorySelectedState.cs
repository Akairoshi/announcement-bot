using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;

public class AnnouncementCategorySelectedState : IConversationState
{
    public readonly Guid CategoryId;
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AnnouncementCategorySelectedState(
        long userId,
        Guid categoryId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        CategoryId = categoryId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
        => await bot.SendMessage(message.Chat.Id, "⚠️ Выберите шаблон из списка выше.", cancellationToken: ct);
}