using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.Application.Queries;
using AnnouncementBot.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class ListAdminRequestsCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ListAdminRequestsCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/list_admin_request";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var requests = await mediator.Send(new GetPendingAdminRequestsQuery(), ct);

        if (!requests.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📥 Активные заявки отсутствуют.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
            return;
        }

        await bot.SendMessage(
            message.Chat.Id,
            $"🗂 Заявок на рассмотрении: {requests.Count}",
            parseMode: ParseMode.Html,
            cancellationToken: ct);

        foreach (var req in requests)
        {
            var typeStr = req.Type == AdminRequestType.Assignment
                ? "🆕 Назначение"
                : "🔄 Переназначение";

            var text = $"📄 <b>Заявка</b> <code>{req.Id}</code>\n" +
                       $"👤 Отправитель: <code>{req.RequesterId}</code>\n" +
                       $"⚙️ Тип: {typeStr}\n" +
                       $"📅 {req.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                       $"💬 <i>{req.Details}</i>";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Одобрить", $"req_rev:approve:{req.Id}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отклонить", $"req_rev:reject:{req.Id}")
                }
            });

            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);
        }
    }
}