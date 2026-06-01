using AnnouncementBot.Application.Commands.Templates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateRemoveConfirmState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _templateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateRemoveConfirmState(long userId, Guid templateId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _templateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        await bot.SendMessage(message.Chat.Id, "⚠️ Нажмите кнопку выше.", cancellationToken: ct);
    }

    public async Task ConfirmAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RemoveTemplateCommand(_templateId), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(chatId, "✅ Шаблон удалён.", cancellationToken: ct);
    }
}