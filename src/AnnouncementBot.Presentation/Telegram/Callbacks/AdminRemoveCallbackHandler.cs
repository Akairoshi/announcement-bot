using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Admin;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class AdminRemoveCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;

    public AdminRemoveCallbackHandler(ConversationStateStorage stateStorage)
    {
        _stateStorage = stateStorage;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("admin_remove_confirm:") ||
           callbackData == "admin_remove_cancel";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        if (data.StartsWith("admin_remove_confirm:"))
        {
            var state = _stateStorage.Get(userId) as RemoveAdminConfirmState;
            if (state is not null)
                await state.ConfirmAsync(bot, chatId, ct);
        }
        else if (data == "admin_remove_cancel")
        {
            _stateStorage.Clear(userId);
            await bot.SendMessage(chatId, "❌ Отменено.", cancellationToken: ct);
        }
    }
}