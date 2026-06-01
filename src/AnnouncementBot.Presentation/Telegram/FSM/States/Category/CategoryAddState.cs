using AnnouncementBot.Application.Commands.Categories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Category;

public class CategoryAddState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public CategoryAddState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var name = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Название не может быть пустым:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new AddCategoryCommand(name, _userId), ct);
            _stateStorage.Clear(_userId);
            await bot.SendMessage(message.Chat.Id, $"✅ Категория «{name}» создана.", cancellationToken: ct);
        }
        catch (InvalidOperationException ex)
        {
            await bot.SendMessage(message.Chat.Id, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}
