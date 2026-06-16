using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.FSM.States;

public class AdminRequestApprovalState : IConversationState
{
    private readonly Guid _requestId;
    private readonly long _superAdminId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRequestApprovalState(
        Guid requestId,
        long superAdminId,
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _requestId = requestId;
        _superAdminId = superAdminId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var categoryName = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(categoryName) || categoryName.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное название категории.\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var adminRequest = await unitOfWork.AdminRequests.GetByIdAsync(_requestId, ct);
        if (adminRequest is null)
        {
            _stateStorage.Clear(_superAdminId);
            await bot.SendMessage(message.Chat.Id, "❌ Заявка не найдена.", cancellationToken: ct);
            return;
        }

        try
        {
            await mediator.Send(new HandleAdminRequestCommand(
                RequestId: _requestId,
                SuperAdminId: _superAdminId,
                IsApproved: true,
                CategoryName: categoryName), ct);

            _stateStorage.Clear(_superAdminId);

            await bot.SendMessage(
                message.Chat.Id,
                $"✅ Заявка одобрена. Пользователю назначена категория <b>\"{categoryName}\"</b>.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            await bot.SendMessage(
                adminRequest.RequesterId,
                $"🎉 Ваша заявка одобрена! Вы назначены <b>Администратором</b>.\n\nВам назначена категория: <b>{categoryName}</b>\n\nИспользуйте /start для обновления меню.",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _stateStorage.Clear(_superAdminId);
            await bot.SendMessage(message.Chat.Id, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}