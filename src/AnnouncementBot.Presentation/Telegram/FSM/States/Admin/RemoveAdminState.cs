using AnnouncementBot.Application.Commands.Users;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Admin;

public class RemoveAdminState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public RemoveAdminState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var username = message.Text?.Trim().TrimStart('@');

        if (string.IsNullOrWhiteSpace(username))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Username не может быть пустым:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var admin = await unitOfWork.Users.GetByUsernameAsync(username, ct);

        if (admin is null || admin.Role != UserRole.Admin)
        {
            await bot.SendMessage(message.Chat.Id, $"❌ Администратор @{username} не найден:", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(_userId, new RemoveAdminConfirmState(_userId, admin.Id, username, _scopeFactory, _stateStorage));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", $"admin_remove_confirm:{admin.Id}"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", "admin_remove_cancel")
            }
        });

        await bot.SendMessage(message.Chat.Id, $"Снять роль администратора у @{username}?", replyMarkup: keyboard, cancellationToken: ct);
    }
}

public class RemoveAdminConfirmState : IConversationState
{
    private readonly long _userId;
    private readonly long _adminId;
    private readonly string _username;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public RemoveAdminConfirmState(long userId, long adminId, string username, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _adminId = adminId;
        _username = username;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
        => await bot.SendMessage(message.Chat.Id, "⚠️ Нажмите кнопку выше.", cancellationToken: ct);

    public async Task ConfirmAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new ChangeUserRoleCommand(_adminId, UserRole.User), ct);
        _stateStorage.Clear(_userId);
        await bot.SendMessage(chatId, $"✅ @{_username} понижен до пользователя.", cancellationToken: ct);
    }
}