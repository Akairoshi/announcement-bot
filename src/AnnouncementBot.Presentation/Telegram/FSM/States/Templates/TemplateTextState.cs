using AnnouncementBot.Application.Commands.Templates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateTextState : IConversationState
{
    private readonly long _userId;
    private readonly string _name;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateTextState(long userId, string name, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _name = name;
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

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new AddTemplateCommand(_name, text, _userId), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(message.Chat.Id, "✅ Шаблон создан.", cancellationToken: ct);
    }
}