using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListCategoryCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ListCategoryCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/list_category";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var categories = await unitOfWork.Categories.GetAllAsync(ct);

        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Категорий пока нет.", cancellationToken: ct);
            return;
        }

        var text = "<b>📂 Список категорий:</b>\n\n" +
                   string.Join("\n", categories.Select((c, i) => $"{i + 1}. {c.Name}"));

        await bot.SendMessage(
            message.Chat.Id,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}
