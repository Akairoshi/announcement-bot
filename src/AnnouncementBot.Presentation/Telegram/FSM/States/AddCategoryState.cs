using AnnouncementBot.Application.Commands.Categories;
using AnnouncementBot.Presentation.Telegram.FSM;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class AddCategoryState : IConversationState
{
    private readonly long _userId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AddCategoryState(long userId, ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _userId = userId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var name = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || name.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное название категории:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var id = await mediator.Send(new AddCategoryCommand(name, _userId), ct);
            _stateStorage.Clear(_userId);

            await bot.SendMessage(
                message.Chat.Id,
                $"✅ Категория <b>\"{name}\"</b> создана.\nID: <code>{id}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _stateStorage.Clear(_userId);
            await bot.SendMessage(message.Chat.Id, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}
