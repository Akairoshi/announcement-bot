using AnnouncementBot.Application.Commands.Categories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Category;

public class CategoryRemoveConfirmState : IConversationState
{
    private readonly long _userId;
    private readonly Guid _categoryId;
    private readonly string _categoryName;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public CategoryRemoveConfirmState(
        long userId,
        Guid categoryId,
        string categoryName,
        IServiceScopeFactory scopeFactory,
        ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _categoryId = categoryId;
        _categoryName = categoryName;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
        => await bot.SendMessage(message.Chat.Id, "⚠️ Нажмите кнопку выше.", cancellationToken: ct);

    public async Task ConfirmAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new RemoveCategoryCommand(_categoryId), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(chatId, $"✅ Категория «{_categoryName}» удалена.", cancellationToken: ct);
    }
}
