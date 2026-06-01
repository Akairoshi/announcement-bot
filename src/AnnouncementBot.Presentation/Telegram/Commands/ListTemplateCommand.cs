using AnnouncementBot.Application.Queries;
using AnnouncementBot.Application.Queries.Templates;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListTemplateCommand : IBotCommand
{
    private readonly IMediator _mediator;

    public string Command => "/list_template";

    public ListTemplateCommand(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        var templates = await _mediator.Send(new GetAdminTemplateQuery(userId), ct);

        if (!templates.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📋 У вас нет шаблонов.",
                cancellationToken: ct);
            return;
        }

        var text = "📋 Ваши шаблоны:\n\n" +
            string.Join("\n\n", templates.Select(t =>
                $"🆔 {t.Id}\n📌 {t.Name}\n📝 {t.Text}"));

        await bot.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }
}