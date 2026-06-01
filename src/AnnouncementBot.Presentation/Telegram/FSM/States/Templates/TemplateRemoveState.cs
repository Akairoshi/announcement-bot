using AnnouncementBot.Application.Commands.Templates;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateRemoveState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateRemoveState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var text = message.Text?.Trim();

        if (text == "да")
        {
            var state = _stateStorage.Get(_userId) as TemplateRemoveState;
        }

        if (!Guid.TryParse(text, out var templateId))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Неверный формат ID. Попробуйте ещё раз:", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var template = await unitOfWork.Templates.GetByIdAsync(templateId, ct);

        if (template is null || template.CreatedById != _userId)
        {
            await bot.SendMessage(message.Chat.Id, "❌ Шаблон не найден.", cancellationToken: ct);
            return;
        }

        _stateStorage.Set(_userId, new TemplateRemoveConfirmState(_userId, templateId, _scopeFactory, _stateStorage));

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", $"template_remove_confirm:{templateId}"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", "template_remove_cancel")
            }
        });

        await bot.SendMessage(
            message.Chat.Id,
            $"Удалить шаблон «{template.Name}»?",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}