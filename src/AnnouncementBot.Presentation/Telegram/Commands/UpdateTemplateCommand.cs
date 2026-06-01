using AnnouncementBot.Presentation.Telegram.FSM.States;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class UpdateTemplateCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Command => "/update_template";

    public UpdateTemplateCommand(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        _stateStorage.Set(userId, new TemplateUpdateIdState(userId, _scopeFactory, _stateStorage));

        await bot.SendMessage(
            message.Chat.Id,
            "🆔 Введите ID шаблона который хотите изменить:",
            cancellationToken: ct);
    }
}