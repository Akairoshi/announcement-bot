using AnnouncementBot.Application.Queries;
using AnnouncementBot.Domain.Enums;
using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using AnnouncementBot.Application.Queries.Users;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ProfileCommand : IBotCommand
{
    private readonly ITelegramBotClient _bot;
    private readonly IMediator _mediator;

    public string Command => "/profile";

    public ProfileCommand(ITelegramBotClient bot, IMediator mediator)
    {
        _bot = bot;
        _mediator = mediator;
    }

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;
        var profile = await _mediator.Send(new GetUserProfileQuery(userId), ct);

        var roleText = profile.Role switch
        {
            UserRole.User => "👤 Пользователь",
            UserRole.Admin => "🔧 Администратор",
            UserRole.SuperAdmin => "👑 Супер Администратор",
            _ => "Неизвестно"
        };

        var categories = profile.AccessibleOrSubscribedCategories.Count > 0
            ? string.Join("\n", profile.AccessibleOrSubscribedCategories.Select(c => $"  • {c}"))
            : "  нет подписок";

        var text = $"""
            👤 Профиль
            
            🆔 ID: {profile.UserId}
            📛 Username: @{profile.UserName ?? "не указан"}
            🎭 Роль: {roleText}
            
            📋 Категории:
            {categories}
            """;

        await bot.SendMessage(message.Chat.Id, text, cancellationToken: ct);
    }
}