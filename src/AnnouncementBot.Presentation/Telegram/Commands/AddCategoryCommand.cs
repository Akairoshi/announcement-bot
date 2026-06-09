using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class AddCategoryCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AddCategoryCommand(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public string Command => "/add_category";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        _stateStorage.Set(userId, new AddCategoryState(userId, _stateStorage, _scopeFactory));

        await bot.SendMessage(
            message.Chat.Id,
            "📂 <b>Создание категории</b>\n\nВведите название новой категории:\n\n" +
            "<i>Для отмены введите /cancel</i>",
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}
