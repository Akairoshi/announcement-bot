using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.AdminRequest;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AdminRequestCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRequestCallbackHandler(
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("admin_request:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data!;
        var chatId = callbackQuery.Message!.Chat.Id;

        if (data == "admin_request:appointment")
        {
            _stateStorage.Set(userId, new AdminRequestReasonState(
                userId,
                AdminRequestType.Assignment,
                null,
                _scopeFactory,
                _stateStorage));

            await bot.SendMessage(
                chatId,
                "📝 Укажите причину заявки:",
                cancellationToken: ct);
        }
        else if (data == "admin_request:reassignment")
        {
            _stateStorage.Set(userId, new AdminRequestTargetState(
                userId,
                _scopeFactory,
                _stateStorage));

            await bot.SendMessage(
                chatId,
                "👤 Введите username (без @) или ID пользователя, которому передаёте роль:",
                cancellationToken: ct);
        }
    }
}