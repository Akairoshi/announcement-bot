using AnnouncementBot.Application.Commands.AdminRequests;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AdminRequestReviewCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public AdminRequestReviewCallbackHandler(
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData) => callbackData.StartsWith("req_rev:");

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var parts = callbackQuery.Data!.Split(':');
        if (parts.Length < 3 || !Guid.TryParse(parts[2], out var requestId))
        {
            await bot.AnswerCallbackQuery(callbackQuery.Id, "❌ Некорректные данные.", cancellationToken: ct);
            return;
        }

        var decision = parts[1];
        var superAdminId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var adminRequest = await unitOfWork.AdminRequests.GetByIdAsync(requestId, ct);
        if (adminRequest is null)
        {
            await bot.SendMessage(chatId, "❌ Заявка не найдена.", cancellationToken: ct);
            return;
        }

        if (decision == "reject")
        {
            try
            {
                await mediator.Send(new HandleAdminRequestCommand(
                    RequestId: requestId,
                    SuperAdminId: superAdminId,
                    IsApproved: false), ct);

                await bot.SendMessage(chatId, "❌ Заявка отклонена.", cancellationToken: ct);

                await bot.SendMessage(
                    adminRequest.RequesterId,
                    "😔 Ваша заявка на получение прав администратора была <b>отклонена</b>.",
                    parseMode: ParseMode.Html,
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
            }

            return;
        }

        if (decision == "approve")
        {
            if (adminRequest.Type == AdminRequestType.Reassignment)
            {
                try
                {
                    var targetId = adminRequest.TargetId!.Value;
                    var requesterId = adminRequest.RequesterId;

                    await mediator.Send(new HandleAdminRequestCommand(
                        RequestId: requestId,
                        SuperAdminId: superAdminId,
                        IsApproved: true), ct);

                    await bot.SendMessage(chatId, "✅ Переназначение выполнено.", cancellationToken: ct);

                    await bot.SendMessage(
                        targetId,
                        "🎉 Вам переданы права <b>Администратора</b>.\nТеперь вы можете создавать объявления. Используйте /start для обновления меню.",
                        parseMode: ParseMode.Html,
                        cancellationToken: ct);

                    await bot.SendMessage(
                        requesterId,
                        "ℹ️ Ваши права администратора были переданы другому пользователю. Вы понижены до роли User.",
                        cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
                }

                return;
            }

            _stateStorage.Set(superAdminId, new AdminRequestApprovalState(
                requestId, superAdminId, _stateStorage, _scopeFactory));

            await bot.SendMessage(
                chatId,
                "📂 Введите название категории для нового администратора\n(можно указать существующую или новую):\n\n<i>Для отмены введите /cancel</i>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
    }
}
