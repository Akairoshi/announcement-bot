using AnnouncementBot.Presentation.Telegram.Callbacks;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;

public class AnnouncementTextState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _categoryId;
    private readonly Guid? _templateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AnnouncementTextState(
        long userId,
        Guid categoryId,
        Guid? templateId,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _categoryId = categoryId;
        _templateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var text = message.Text?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "⚠️ Пожалуйста, введите корректный текстовый формат объявления.",
                cancellationToken: ct);
            return;
        }

        var callbackHandler = new AnnouncementCallbackHandler(_stateStorage, _scopeFactory);

        await callbackHandler.ShowConfirmationAsync(
            bot,
            _userId,
            message.Chat.Id,
            text,
            _categoryId,
            _templateId,
            ct);
    }
}