using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.WebApi.Telegram.FSM;
using AnnouncementBot.WebApi.Telegram.FSM.States;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.Commands;

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
                    "👑 Вы являетесь Супер Администратором.",
                    cancellationToken: ct);
                return;

            case UserRole.Admin:
                _stateStorage.Set(userId, new AdminRequestState(
                    AdminRequestType.Reassignment, userId, _stateStorage, _scopeFactory));

                await bot.SendMessage(
                    message.Chat.Id,
                    "🔄 <b>Переназначение прав администратора</b>\n\nВведите ID или @username пользователя, которому хотите передать роль:\n\nДля отмены введите /cancel",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
                return;

            default:
                _stateStorage.Set(userId, new AdminRequestState(
                    AdminRequestType.Assignment, userId, _stateStorage, _scopeFactory));

                await bot.SendMessage(
                    message.Chat.Id,
                    "📝 <b>Заявка на права Администратора</b>\n\nОпишите причину, почему вам необходим доступ к созданию объявлений:\n\nДля отмены введите /cancel",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
                return;
        }
    }
}