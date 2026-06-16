
using Telegram.Bot.Types;

namespace AnnouncementBot.WebApi.Middlewares
{
    public interface IBotMiddleware
    {
        Task InvokeAsync(Update update, Func<Task> next, CancellationToken ct);
    }
}
