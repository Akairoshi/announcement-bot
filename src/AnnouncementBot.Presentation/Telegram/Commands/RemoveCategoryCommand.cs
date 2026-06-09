using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class RemoveCategoryCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RemoveCategoryCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Command => "/remove_category";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var categories = await unitOfWork.Categories.GetAllAsync(ct);

        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Категорий нет.", cancellationToken: ct);
            return;
        }

        var buttons = categories
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData(c.Name, $"cat_del_sel:{c.Id}") })
            .ToList();

        await bot.SendMessage(
            message.Chat.Id,
            "🗑 <b>Выберите категорию для удаления:</b>\n\n" +
            "<i>Для отмены введите /cancel</i>",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}
