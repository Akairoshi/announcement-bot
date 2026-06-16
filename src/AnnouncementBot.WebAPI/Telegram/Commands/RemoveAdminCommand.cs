using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class RemoveAdminCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public RemoveAdminCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Command => "/remove_admin";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var admins = await unitOfWork.Users.GetAllAdminsAsync(ct);
        var regularAdmins = admins.Where(a => a.Role == UserRole.Admin).ToList();

        if (!regularAdmins.Any())
        {
            await bot.SendMessage(message.Chat.Id, "📭 Администраторы отсутствуют.", cancellationToken: ct);
            return;
        }

        var buttons = regularAdmins
            .Select(a =>
            {
                var label = a.UserName is not null ? $"@{a.UserName}" : a.Id.ToString();
                return new[] { InlineKeyboardButton.WithCallbackData(label, $"adm_del_sel:{a.Id}") };
            })
            .ToList();

        await bot.SendMessage(
            message.Chat.Id,
            "⚙️ <b>Выберите администратора для удаления:</b>\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons),
            cancellationToken: ct);
    }
}