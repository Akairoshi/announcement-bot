using AnnouncementBot.Application.Queries;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListCategoryCommand : IBotCommand
{
    private readonly IMediator _mediator;

    public string Command => "/list_category";

    public ListCategoryCommand(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var categories = await _mediator.Send(new GetAllCategoriesQuery(), ct);

        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📋 Категорий пока нет.", cancellationToken: ct);
            return;
        }

        var text = "📋 Категории:\n\n" +
            string.Join("\n", categories.Select(c => $"• {c.Name} (ID: {c.Id})"));

        await bot.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }
}
