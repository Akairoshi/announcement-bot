using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateNameState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateNameState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var name = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Название не может быть пустым:", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(_userId, new TemplateTextState(_userId, name, _scopeFactory, _stateStorage));

        await bot.SendMessage(message.Chat.Id, "📝 Введите текст шаблона:", cancellationToken: ct);
    }
}