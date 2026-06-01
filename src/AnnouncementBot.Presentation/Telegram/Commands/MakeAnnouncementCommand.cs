using AnnouncementBot.Application.Queries.Templates;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class MakeAnnouncementCommand : IBotCommand
{
    private readonly IMediator _mediator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public string Command => "/make_announcement";

    public MakeAnnouncementCommand(IMediator mediator, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _mediator = mediator;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        var templates = await _mediator.Send(new GetAdminTemplateQuery(userId), ct);

        if (!templates.Any())
        {
            _stateStorage.Set(userId, new AnnouncementTextState(userId, null, _scopeFactory, _stateStorage));
            await bot.SendMessage(
                message.Chat.Id,
                "📝 Введите текст объявления:",
                replyMarkup: new ReplyKeyboardRemove(), // ← добавь
                cancellationToken: ct);
            return;
        }

        var buttons = templates
            .Select(t => new[] { InlineKeyboardButton.WithCallbackData(t.Name, $"announcement_template:{t.Id}") })
            .ToList();

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("✏️ Без шаблона", "announcement_template:none") });

        await bot.SendMessage(
            message.Chat.Id,
            "📋 Выберите шаблон или продолжите без него:",
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}
