using AnnouncementBot.Application.Queries.AdminRequests;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListAdminRequestCommand : IBotCommand
{
    private readonly IMediator _mediator;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Command => "/list_admin_request";

    public ListAdminRequestCommand(IMediator mediator, ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _mediator = mediator;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var requests = await _mediator.Send(new GetPendingAdminRequestsQuery(), ct);

        if (!requests.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📋 Pending заявок нет.", cancellationToken: ct);
            return;
        }

        foreach (var request in requests)
        {
            var typeText = request.Type == Domain.Enums.AdminRequestType.Assignment
                ? "📋 Назначение"
                : "🔄 Переназначение";

            var text = $"""
                {typeText}
                👤 От: {request.RequesterId}
                📝 Причина: {request.Details ?? "не указана"}
                🕐 Дата: {request.CreatedAt:dd.MM.yyyy HH:mm}
                """;

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Одобрить", $"admin_request_review:approve:{request.Id}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"admin_request_review:reject:{request.Id}")
                }
            });

            await bot.SendMessage(message.Chat.Id, text, replyMarkup: keyboard, cancellationToken: ct);
        }
    }
}
