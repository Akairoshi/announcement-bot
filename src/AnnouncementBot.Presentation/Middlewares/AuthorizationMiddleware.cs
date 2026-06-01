using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Middlewares;

public class AuthorizationMiddleware : IBotMiddleware
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ITelegramBotClient _bot;

    public AuthorizationMiddleware(IServiceProvider serviceProvider, ITelegramBotClient bot)
    {
        _serviceProvider = serviceProvider;
        _bot = bot;
    }

    public async Task InvokeAsync(Update update, Func<Task> next, CancellationToken ct)
    {
        var text = update.Message?.Text?.Trim();
        var chatId = update.Message?.Chat.Id ?? update.CallbackQuery?.Message?.Chat.Id;
        var userId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id;

        if (text is null || userId is null)
        {
            await next();
            return;
        }

        // команды доступные всем — проверку не делаем
        if (text.StartsWith("/start"))
        {
            await next();
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId.Value, ct);

        if (user is null)
        {
            await next();
            return;
        }

        // команды только для Admin и SuperAdmin
        var adminCommands = new[] { "/make_announcement", "/list_template", "/add_template", "/update_template", "/remove_template" };
        var superAdminCommands = new[] { "/list_admin_request", "/list_admin", "/remove_admin", "/list_category", "/add_category", "/update_category", "/remove_category" };

        if (adminCommands.Any(c => text.StartsWith(c)) && user.Role == UserRole.User)
        {
            await _bot.SendMessage(chatId!, "⛔ У вас нет прав для этой команды.", cancellationToken: ct);
            return;
        }

        if (superAdminCommands.Any(c => text.StartsWith(c)) && user.Role != UserRole.SuperAdmin)
        {
            await _bot.SendMessage(chatId!, "⛔ Эта команда доступна только SuperAdmin.", cancellationToken: ct);
            return;
        }

        await next();
    }
}