using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class UpdateTemplateCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateTemplateCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Command => "/update_template";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var templates = await unitOfWork.Templates.GetByAdminIdAsync(userId, ct);

        if (!templates.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 У вас нет шаблонов.", cancellationToken: ct);
            return;
        }

        var buttons = templates
            .Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.Name, $"tpl_upd:{t.Id}") })
            .ToList();

        await bot.SendMessage(
            message.Chat.Id,
            "📝 <b>Выберите шаблон для редактирования:</b>\n\n" +
            "<i>Для отмены введите /cancel</i>",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}
