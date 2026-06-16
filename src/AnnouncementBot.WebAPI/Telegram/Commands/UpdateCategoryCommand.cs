using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.WebApi.Telegram.FSM;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class UpdateCategoryCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateCategoryCommand(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public string Command => "/update_category";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var categories = await unitOfWork.Categories.GetAllAsync(ct);

        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Категории отсутствуют.", cancellationToken: ct);
            return;
        }

        var buttons = categories
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.Name, $"cat_upd:{c.Id}") })
            .ToList();

        await bot.SendMessage(
            message.Chat.Id,
            "📂 <b>Выберите категорию для изменения:</b>\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}