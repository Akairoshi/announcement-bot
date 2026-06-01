using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM.States;
using AnnouncementBot.Presentation.Telegram.FSM;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class TemplateRemoveCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;

    public TemplateRemoveCallbackHandler(ConversationStateStorage stateStorage)
    {
        _stateStorage = stateStorage;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("template_remove_confirm:") ||
           callbackData == "template_remove_cancel";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        if (data.StartsWith("template_remove_confirm:"))
        {
            var state = _stateStorage.Get(userId) as TemplateRemoveConfirmState;
            if (state is not null)
                await state.ConfirmAsync(bot, chatId, ct);
        }
        else if (data == "template_remove_cancel")
        {
            _stateStorage.Clear(userId);
            await bot.SendMessage(chatId, "❌ Удаление отменено.", cancellationToken: ct);
        }
    }
}