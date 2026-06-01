using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateUpdateNameState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _templateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateUpdateNameState(long userId, Guid templateId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _templateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();
        var newName = input == "/skip" ? null : input;

        _stateStorage.Set(_userId, new TemplateUpdateTextState(_userId, _templateId, newName, _scopeFactory, _stateStorage));

        await bot.SendMessage(
            message.Chat.Id,
            "📝 Введите новый текст шаблона (или /skip чтобы оставить):",
            cancellationToken: ct);
    }
}