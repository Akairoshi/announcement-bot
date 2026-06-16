using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

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
            await bot.SendMessage(message.Chat.Id, "📭 Шаблоны отсутствуют.", cancellationToken: ct);
            return;
        }

        var buttons = templates
            .Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.Name, $"tpl_upd:{t.Id}") })
            .ToList();

        await bot.SendMessage(
            message.Chat.Id,
            "📝 <b>Выберите шаблон для изменения:</b>\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}