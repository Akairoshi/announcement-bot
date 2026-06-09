using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Application.Queries.Templates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListTemplateCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ListTemplateCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/list_template";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var templates = await mediator.Send(new GetAdminTemplateQuery(userId), ct);

        if (!templates.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📭 <b>У вас пока нет шаблонов.</b>\nИспользуйте /add_template чтобы создать.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"📋 <b>Ваши шаблоны ({templates.Count}):</b>",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        foreach (var t in templates)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🗑 Удалить", $"tpl_del_sel:{t.Id}")
                }
            });

            await bot.SendMessage(
                message.Chat.Id,
                $"✨ <b>{t.Name}</b>\n\n<code>{t.Text}</code>",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}
