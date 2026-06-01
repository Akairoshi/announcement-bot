using AnnouncementBot.Application.Commands.AdminRequests;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;

public class AdminRequestApproveState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _requestId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AdminRequestApproveState(
        long userId,
        Guid requestId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _requestId = requestId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var categoryName = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(categoryName))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "⚠️ Название категории не может быть пустым:",
                cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new HandleAdminRequestCommand(
            _requestId,
            _userId,
            IsApproved: true,
            CategoryName: categoryName), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(
            message.Chat.Id,
            $"✅ Заявка одобрена. Администратору назначена категория «{categoryName}».",
            cancellationToken: ct);
    }
}
