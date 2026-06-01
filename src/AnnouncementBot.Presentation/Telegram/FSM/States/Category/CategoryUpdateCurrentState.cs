using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Category;

public class CategoryUpdateCurrentState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public CategoryUpdateCurrentState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var currentName = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(currentName))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Название не может быть пустым:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var category = await unitOfWork.Categories.GetByNameAsync(currentName, ct);

        if (category is null)
        {
            await bot.SendMessage(message.Chat.Id, $"❌ Категория «{currentName}» не найдена:", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(_userId, new CategoryUpdateNewNameState(
            _userId, category.Id, _scopeFactory, _stateStorage));

        await bot.SendMessage(message.Chat.Id, "📝 Введите новое название:", cancellationToken: ct);
    }
}
