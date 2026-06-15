using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class AddTemplateCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AddTemplateCommand(ConversationStateStorage stateStorage, IServiceScopeFactory _scopeFactory)
    {
        _stateStorage = stateStorage;
        this._scopeFactory = _scopeFactory;
    }

    public string Command => "/add_template";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        _stateStorage.Set(userId, new AddTemplateState(userId, _stateStorage, _scopeFactory));

        await bot.SendMessage(
            message.Chat.Id,
            "📝 <b>Создание шаблона</b>\n\nВведите название шаблона:\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}