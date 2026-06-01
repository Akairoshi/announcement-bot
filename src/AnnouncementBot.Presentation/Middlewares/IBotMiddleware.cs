
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Middlewares
{
    public interface IBotMiddleware
    {
        Task InvokeAsync(Update update, Func<Task> next, CancellationToken ct);
    }
}
