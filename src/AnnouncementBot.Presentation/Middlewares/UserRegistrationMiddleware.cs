using AnnouncementBot.Application.Commands.Users;
using MediatR;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Middlewares;

public class UserRegistrationMiddleware : IBotMiddleware
{
    private readonly IMediator _mediator;

    public UserRegistrationMiddleware(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task InvokeAsync(Update update, Func<Task> next, CancellationToken ct)
    {
        var telegramUser = update.Message?.From ?? update.CallbackQuery?.From;

        if (telegramUser is not null)
        {
            await _mediator.Send(new EnsureUserExistsCommand(
                telegramUser.Id,
                telegramUser.Username), ct);
        }

        await next();
    }
}