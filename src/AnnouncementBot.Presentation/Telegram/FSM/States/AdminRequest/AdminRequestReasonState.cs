using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;

public class AdminRequestReasonState : IConversationState
{
    private readonly long _userId;
    private readonly AdminRequestType _requestType;
    private readonly long? _targetId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AdminRequestReasonState(
        long userId,
        AdminRequestType requestType,
        long? targetId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _requestType = requestType;
        _targetId = targetId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var reason = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "⚠️ Причина не может быть пустой. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new CreateAdminRequestCommand(
            _userId,
            _requestType,
            reason,
            _targetId), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(
            message.Chat.Id,
            "✅ Заявка отправлена. SuperAdmin рассмотрит её в ближайшее время.",
            cancellationToken: ct);
    }
}