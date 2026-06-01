using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;

public class AdminRequestTargetState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AdminRequestTargetState(
        long userId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var username = message.Text?.Trim().TrimStart('@');

        if (string.IsNullOrWhiteSpace(username))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "⚠️ Username не может быть пустым. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var target = await unitOfWork.Users.GetByUsernameAsync(username, ct);

        if (target is null)
        {
            await bot.SendMessage(
                message.Chat.Id,
                $"❌ Пользователь @{username} не найден. Попробуйте ещё раз:",
                cancellationToken: ct);
            return;
        }


        _stateStorage.Set(_userId, new AdminRequestReasonState(
            _userId,
            AdminRequestType.Reassignment,
            target.Id,
            _scopeFactory,
            _stateStorage));

        await bot.SendMessage(
            message.Chat.Id,
            $"👤 Цель: @{username}\n\n📝 Укажите причину переназначения:",
            cancellationToken: ct);
    }
}