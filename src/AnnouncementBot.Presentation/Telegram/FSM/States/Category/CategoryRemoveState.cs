using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using AnnouncementBot.Presentation.Telegram.FSM.States.Category;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Category;

public class CategoryRemoveState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public CategoryRemoveState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public ConversationStateStorage StateStorage => _stateStorage;

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var name = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Название не может быть пустым:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var category = await unitOfWork.Categories.GetByNameAsync(name, ct);

        if (category is null)
        {
            await bot.SendMessage(message.Chat.Id, $"❌ Категория «{name}» не найдена:", cancellationToken: ct);
            return;
        }

        StateStorage.Set(_userId, new CategoryRemoveConfirmState(
            _userId, category.Id, name, _scopeFactory, StateStorage));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", $"category_remove_confirm:{category.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", "category_remove_cancel")
            }
        });

        await bot.SendMessage(
            message.Chat.Id,
            $"Удалить категорию «{name}»?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}
