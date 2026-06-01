using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Application.Queries;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListAdminCommand : IBotCommand
{
    private readonly IMediator _mediator;

    public string Command => "/list_admin";

    public ListAdminCommand(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var admins = await _mediator.Send(new GetAllAdminsQuery(), ct);

        if (!admins.Any())
        {
            await bot.SendMessage(message.Chat.Id, "👥 Администраторов пока нет.", cancellationToken: ct);
            return;
        }

        var text = "👥 Администраторы:\n\n" +
            string.Join("\n", admins.Select(a =>
                $"• @{a.UserName ?? "без username"} (ID: {a.Id})"));

        await bot.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }
}
