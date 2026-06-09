using AnnouncementBot.Presentation.Telegram.FSM;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class AnnouncementCategorySelectedState : IConversationState
{
    public Guid CategoryId { get; }

    public AnnouncementCategorySelectedState(Guid categoryId)
    {
        CategoryId = categoryId;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
        => await bot.SendMessage(message.Chat.Id, "⚠️ Выберите шаблон из списка выше.", cancellationToken: ct);
}
