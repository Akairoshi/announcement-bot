using AnnouncementBot.Application.Commands.Templates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateUpdateTextState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _templateId;
    private readonly string? _newName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateUpdateTextState(long userId, Guid templateId, string? newName, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _templateId = templateId;
        _newName = newName;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();
        var newText = input == "/skip" ? null : input;

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new UpdateTemplateCommand(_templateId, _newName, newText), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(message.Chat.Id, "✅ Шаблон обновлён.", cancellationToken: ct);
    }
}