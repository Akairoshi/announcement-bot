using AnnouncementBot.Application.Queries;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListAnnouncementCommand : IBotCommand
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;

    public string Command => "/list_announcement";

    public ListAnnouncementCommand(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);

        if (user is null) return;

        var announcements = await _mediator.Send(
            new GetAnnouncementsQuery(userId, user.Role), ct);

        if (!announcements.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📭 Объявлений пока нет.",
                cancellationToken: ct);
            return;
        }

        foreach (var announcement in announcements.Take(10))
        {
            var text = $"""
                📢 {announcement.CategoryName}
                🕐 {announcement.CreatedAt:dd.MM.yyyy HH:mm}
                
                {announcement.Text}
                """;

            await bot.SendMessage(message.Chat.Id, text, cancellationToken: ct);
        }
    }
}