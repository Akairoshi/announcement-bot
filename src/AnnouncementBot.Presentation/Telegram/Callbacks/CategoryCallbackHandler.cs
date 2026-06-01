using AnnouncementBot.Presentation.Telegram.Callbacks.Interfaces;
using AnnouncementBot.Presentation.Telegram.FSM;
using AnnouncementBot.Presentation.Telegram.FSM.States.Category;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.Callbacks;

public class CategoryCallbackHandler : ICallbackHandler
{
    private readonly ConversationStateStorage _stateStorage;

    public CategoryCallbackHandler(ConversationStateStorage stateStorage)
    {
        _stateStorage = stateStorage;
    }

    public bool CanHandle(string callbackData)
        => callbackData.StartsWith("category_remove_confirm:") ||
           callbackData == "category_remove_cancel";

    public async Task HandleAsync(ITelegramBotClient bot, CallbackQuery callbackQuery, CancellationToken ct)
    {
        var userId = callbackQuery.From.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data!;

        if (data.StartsWith("category_remove_confirm:"))
        {
            var state = _stateStorage.Get(userId) as CategoryRemoveConfirmState;
            if (state is not null)
                await state.ConfirmAsync(bot, chatId, ct);
        }
        else if (data == "category_remove_cancel")
        {
            _stateStorage.Clear(userId);
            await bot.SendMessage(chatId, "❌ Удаление отменено.", cancellationToken: ct);
        }
    }
}