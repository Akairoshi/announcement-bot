using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Category;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class UpdateCategoryCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public string Command => "/update_category";

    public UpdateCategoryCommand(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        _stateStorage.Set(message.From!.Id, new CategoryUpdateCurrentState(message.From.Id, _scopeFactory, _stateStorage));
        await bot.SendMessage(message.Chat.Id, "📝 Введите текущее название категории:", cancellationToken: ct);
    }
}
