using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class MakeAnnouncementCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public string Command => "/make_announcement";

    public MakeAnnouncementCommand(IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        _stateStorage.Clear(userId);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);

        if (!accesses.Any())
        {
            await bot.SendMessage(message.Chat.Id, "❌ У вас нет доступных категорий.", cancellationToken: ct);
            return;
        }

        var buttons = new List<InlineKeyboardButton[]>();
        foreach (var access in accesses)
        {
            var category = await unitOfWork.Categories.GetByIdAsync(access.CategoryId, ct);
            if (category is not null)
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(category.Name, $"ann_cat:{category.Id}")
                });
        }

        await bot.SendMessage(
            message.Chat.Id,
            "📂 Выберите категорию для объявления:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}