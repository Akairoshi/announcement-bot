using AnnouncementBot.Application.Commands.Templates;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.WebApi.Telegram.Callbacks.Interfaces;
using AnnouncementBot.WebApi.Telegram.FSM;
using AnnouncementBot.WebApi.Telegram.FSM.States;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Callbacks;

public class TemplateCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public TemplateCallbackHandler(ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public bool CanHandle(string callbackData) =>
        callbackData.StartsWith("tpl_upd:") ||
        callbackData.StartsWith("tpl_del_sel:") ||
        callbackData.StartsWith("tpl_del_yes:") ||
        callbackData == "tpl_del_no";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var data = callbackQuery.Data!;
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var messageId = callbackQuery.Message.MessageId;

        if (data.StartsWith("tpl_upd:"))
        {
            var templateId = Guid.Parse(data["tpl_upd:".Length..]);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            _stateStorage.Set(userId, new UpdateTemplateState(templateId, userId, _stateStorage, _scopeFactory));

            await bot.SendMessage(
                chatId,
                "📝 <b>Редактирование шаблона</b>\n\nВведите новое название шаблона:\nДля пропуска изменения введите /skip\n\nДля отмены введите /cancel",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("tpl_del_sel:"))
        {
            var templateId = Guid.Parse(data["tpl_del_sel:".Length..]);

            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var template = await unitOfWork.Templates.GetByIdAsync(templateId, ct);

            if (template is null)
            {
                await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
                await bot.SendMessage(chatId, "❌ Шаблон не найден.", cancellationToken: ct);
                return;
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("✅ Да, удалить", $"tpl_del_yes:{templateId}"),
                    InlineKeyboardButton.WithCallbackData("❌ Отмена", "tpl_del_no")
                }
            });

            await bot.EditMessageText(
                chatId,
                messageId,
                $"🗑 Удалить шаблон <b>\"{template.Name}\"</b>?",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: ct);

            return;
        }

        if (data.StartsWith("tpl_del_yes:"))
        {
            var templateId = Guid.Parse(data["tpl_del_yes:".Length..]);

            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(new RemoveTemplateCommand(templateId, userId), ct);
                await bot.SendMessage(chatId, "✅ Шаблон удален.", cancellationToken: ct);
            }
            catch (Exception ex)
            {
                await bot.SendMessage(chatId, $"❌ {ex.Message}", cancellationToken: ct);
            }

            return;
        }

        if (data == "tpl_del_no")
        {
            await bot.EditMessageReplyMarkup(chatId, messageId, replyMarkup: null, cancellationToken: ct);
            await bot.SendMessage(chatId, "❌ Удаление отменено.", cancellationToken: ct);
        }
    }
}