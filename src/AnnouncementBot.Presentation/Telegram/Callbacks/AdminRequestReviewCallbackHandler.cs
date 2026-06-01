using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

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
        if (parts.Length < 3) return;

        var action = parts[1];
        var requestId = Guid.Parse(parts[2]);

        if (action == "approve")
        {
            _stateStorage.Set(userId, new AdminRequestApproveState(
                userId, requestId, _scopeFactory, _stateStorage));

            await bot.SendMessage(
                chatId,
                "📂 Введите название категории для назначения администратора:",
                cancellationToken: ct);
        }
        else if (action == "reject")
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new HandleAdminRequestCommand(
                requestId,
                userId,
                IsApproved: false), ct);

            await bot.SendMessage(chatId, "❌ Заявка отклонена.", cancellationToken: ct);
        }
    }
}
