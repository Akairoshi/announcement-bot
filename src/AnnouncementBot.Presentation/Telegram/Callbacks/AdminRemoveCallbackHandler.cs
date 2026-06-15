using AnnouncementBot.Application.Commands.Users;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AdminRemoveCallbackHandler : ICallbackHandler
{
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRemoveCallbackHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData) =>
        callbackData.StartsWith("adm_del_sel:") ||
        callbackData.StartsWith("adm_del_yes:") ||
        callbackData == "adm_del_no";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data!;
        var superAdminId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        if (data.StartsWith("adm_del_sel:"))
        {
            var targetId = long.Parse(data["adm_del_sel:".Length..]);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var user = await unitOfWork.Users.GetByIdAsync(targetId, ct);

            if (user is null)
            {
                await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
                await bot.SendMessage(chatId, "❌ Пользователь не найден.", cancellationToken: ct);
                return;
            }

            var label = user.UserName is not null ? $"@{user.UserName}" : $"ID {user.Id}";
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"adm_del_yes:{targetId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "adm_del_no")
                }
            });

            await bot.EditMessageText(
                chatId,
                messageId,
                $"⚙️ Снять права администратора у <b>{label}</b>?",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("adm_del_yes:"))
        {
            var targetId = long.Parse(data["adm_del_yes:".Length..]);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new ChangeUserRoleCommand(targetId, UserRole.User, superAdminId), ct);
                await bot.SendMessage(chatId, "✅ Администратор понижен до роли Пользователь.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
            }

            return;
        }

        if (data == "adm_del_no")
        {
            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
            await bot.SendMessage(chatId, "❌ Удаление отменено.", cancellationToken: ct);
        }
    }
}