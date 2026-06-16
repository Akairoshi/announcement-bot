using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using User = AnnouncementBot.Domain.Entities.User;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.FSM.States;

public class AdminRequestState : IConversationState
{
    private enum Step { WaitingTarget, WaitingReason }

    private readonly AdminRequestType _requestType;
    private readonly long _userId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    private Step _step;
    private long? _targetId;

    public AdminRequestState(
        AdminRequestType requestType,
        long userId,
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _requestType = requestType;
        _userId = userId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
        _step = requestType == AdminRequestType.Reassignment ? Step.WaitingTarget : Step.WaitingReason;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(input) || input.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное текстовое значение.\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        if (_step == Step.WaitingTarget)
        {
            await HandleTargetInputAsync(bot, message.Chat.Id, input, ct);
            return;
        }

        await HandleReasonInputAsync(bot, message.Chat.Id, input, ct);
    }

    private async Task HandleTargetInputAsync(ITelegramBotClient bot, long chatId, string input, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        User? targetUser = null;

        if (long.TryParse(input, out var targetIdParsed))
            targetUser = await unitOfWork.Users.GetByIdAsync(targetIdParsed, ct);
        else
            targetUser = await unitOfWork.Users.GetByUsernameAsync(input.TrimStart('@'), ct);

        if (targetUser is null)
        {
            await bot.SendMessage(chatId, "❌ Пользователь не найден. Введите корректный ID или @username:\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        if (targetUser.Id == _userId)
        {
            await bot.SendMessage(chatId, "❌ Нельзя передать роль самому себе. Введите другого пользователя:\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        if (targetUser.Role == UserRole.SuperAdmin)
        {
            await bot.SendMessage(chatId, "❌ Нельзя указать Супер Администратора в качестве цели. Введите другого пользователя:\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        if (targetUser.Role == UserRole.Admin)
        {
            await bot.SendMessage(chatId, "❌ Этот пользователь уже является администратором. Введите другого пользователя:\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        _targetId = targetUser.Id;
        _step = Step.WaitingReason;

        var label = targetUser.UserName is not null ? $"@{targetUser.UserName}" : $"ID {targetUser.Id}";

        await bot.SendMessage(
            chatId,
            $"👤 Целевой пользователь: <b>{label}</b>\n\nТеперь введите причину переназначения:\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }

    private async Task HandleReasonInputAsync(ITelegramBotClient bot, long chatId, string reason, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new CreateAdminRequestCommand(
                RequesterId: _userId,
                RequestType: _requestType,
                Reason: reason,
                TargetId: _targetId), ct);

            _stateStorage.Clear(_userId);

            await bot.SendMessage(
                chatId,
                "✅ Заявка отправлена. Супер-администратор рассмотрит её в ближайшее время.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _stateStorage.Clear(_userId);
            await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}