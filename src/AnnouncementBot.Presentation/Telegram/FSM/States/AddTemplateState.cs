using AnnouncementBot.Application.Commands.Templates;
using AnnouncementBot.Presentation.Telegram.FSM;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class AddTemplateState : IConversationState
{
    private enum Step { WaitingName, WaitingText }

    private readonly long _userId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;
    private Step _step = Step.WaitingName;
    private string _name = string.Empty;

    public AddTemplateState(long userId, ConversationStateStorage stateStorage, IServiceScopeFactory scopeFactory)
    {
        _userId = userId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var input = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(input) || input.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное текстовое значение:", cancellationToken: ct);
            return;
        }

        if (_step == Step.WaitingName)
        {
            _name = input;
            _step = Step.WaitingText;

            await bot.SendMessage(
                message.Chat.Id,
                $"📝 Название: <b>{_name}</b>\n\nТеперь введите текст шаблона.\nДля переменных используйте фигурные скобки: <code>{{Имя}}</code>, <code>{{Дата}}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);

            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var id = await mediator.Send(new AddTemplateCommand(_name, input, _userId), ct);
            _stateStorage.Clear(_userId);

            await bot.SendMessage(
                message.Chat.Id,
                $"✅ Шаблон <b>\"{_name}\"</b> создан.\nID: <code>{id}</code>",
                parseMode: ParseMode.Html,
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _stateStorage.Clear(_userId);
            await bot.SendMessage(message.Chat.Id, $"❌ {ex.Message}", cancellationToken: ct);
        }
    }
}
