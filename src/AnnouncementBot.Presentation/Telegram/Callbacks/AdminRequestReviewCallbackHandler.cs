using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AdminRequestReviewCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRequestReviewCallbackHandler(
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("admin_request_review:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        var parts = data.Split(':');
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var requestId))
        {
            await bot.SendMessage(chatId, "❌ Некорректная заявка.", cancellationToken: ct);
            return;
        }

        var action = parts[1];

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var adminRequest = await unitOfWork.AdminRequests.GetByIdAsync(requestId, ct);

        if (adminRequest is null)
        {
            await bot.SendMessage(chatId, "❌ Заявка не найдена.", cancellationToken: ct);
            return;
        }

        if (adminRequest.Status != AdminRequestStatus.Pending)
        {
            await bot.SendMessage(chatId, "⚠️ Эта заявка уже была обработана.", cancellationToken: ct);
            return;
        }

        if (action == "approve")
        {
            if (adminRequest.Type == AdminRequestType.Assignment)
            {
                _stateStorage.Set(userId, new AdminRequestApproveState(
                    userId, requestId, _scopeFactory, _stateStorage));

                await bot.SendMessage(
                    chatId,
                    "📂 Введите название категории для назначения администратора:",
                    cancellationToken: ct);

                return;
            }

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new HandleAdminRequestCommand(
                requestId,
                userId,
                IsApproved: true), ct);

            await bot.SendMessage(chatId, "✅ Заявка на переназначение одобрена.", cancellationToken: ct);
        }
        else if (action == "reject")
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new HandleAdminRequestCommand(
                requestId,
                userId,
                IsApproved: false), ct);

            await bot.SendMessage(chatId, "❌ Заявка отклонена.", cancellationToken: ct);
            await bot.SendMessage(adminRequest.RequesterId, "❌ Ваша заявка на администрирование отклонена.", cancellationToken: ct);
        }
    }
}
