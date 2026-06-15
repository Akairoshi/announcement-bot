using AnnouncementBot.Application.Commands.Templates;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class UpdateTemplateState : IConversationState
{
    private enum Step { WaitingName, WaitingText }

    private readonly Guid _templateId;
    private readonly long _userId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private Step _step = Step.WaitingName;
    private string? _newName;

    public UpdateTemplateState(
        Guid templateId,
        long userId,
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _templateId = templateId;
        _userId = userId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(input) || input.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное текстовое значение.\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        if (_step == Step.WaitingName)
        {
            _newName = input;
            _step = Step.WaitingText;

            await bot.SendMessage(
                message.Chat.Id,
                $"📝 Новое название: <b>{_newName}</b>\n\nТеперь введите новый текст шаблона:\n\nДля отмены введите /cancel",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new UpdateTemplateCommand(_templateId, _newName, input, _userId), ct);
            _stateStorage.Clear(_userId);

            await bot.SendMessage(
                message.Chat.Id,
                "✅ Шаблон обновлён.",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _stateStorage.Clear(_userId);
            await bot.SendMessage(message.Chat.Id, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}