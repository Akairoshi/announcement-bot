using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class ListAdminCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ListAdminCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/list_admin";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var admins = await unitOfWork.Users.GetAllAdminsAsync(ct);

        if (!admins.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Администраторы отсутствуют.", cancellationToken: ct);
            return;
        }

        var lines = admins.Select((a, i) =>
        {
            var username = a.UserName is not null ? $"@{a.UserName}" : $"<code>{a.Id}</code>";
            var role = a.Role == UserRole.SuperAdmin ? " 👑" : "";
            return $"{i + 1}. {username}{role}";
        });

        var text = "⚙️ <b>Список администраторов:</b>\n\n" + string.Join("\n", lines);

        await bot.SendMessage(
            message.Chat.Id,
            text,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}