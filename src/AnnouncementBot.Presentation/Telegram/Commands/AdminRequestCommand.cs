using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class AdminRequestCommand : IBotCommand
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRequestCommand(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public string Command => "/admin_request";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);

        if (user is null) return;

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
                await bot.SendMessage(
                    message.Chat.Id,
                    "ℹ️ Вы уже являетесь Супер Администратором.",
                    cancellationToken: ct);
                return;

            case UserRole.Admin:
                _stateStorage.Set(userId, new AdminRequestState(
                    AdminRequestType.Reassignment, userId, _stateStorage, _scopeFactory));

                await bot.SendMessage(
                    message.Chat.Id,
                    "🔄 <b>Переназначение прав администратора</b>\n\n" +
                    "Введите ID или @username пользователя, которому хотите передать свою роль:\n\n" +
                    "<i>Для отмены введите /cancel</i>",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
                return;

            default:
                _stateStorage.Set(userId, new AdminRequestState(
                    AdminRequestType.Assignment, userId, _stateStorage, _scopeFactory));

                await bot.SendMessage(
                    message.Chat.Id,
                    "📝 <b>Заявка на права Администратора</b>\n\n" +
                    "Опишите причину, почему вам необходим доступ к созданию объявлений:\n\n" +
                    "<i>Для отмены введите /cancel</i>",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
                return;
        }
    }
}
