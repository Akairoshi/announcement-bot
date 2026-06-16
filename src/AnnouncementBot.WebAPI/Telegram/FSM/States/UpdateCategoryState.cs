using AnnouncementBot.Application.Commands.Categories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.FSM.States;

public class UpdateCategoryState : IConversationState
{
    private readonly Guid _categoryId;
    private readonly long _userId;
    private readonly ConversationStateStorage _stateStorage;
    private readonly IServiceScopeFactory _scopeFactory;

    public UpdateCategoryState(
        Guid categoryId,
        long userId,
        ConversationStateStorage stateStorage,
        IServiceScopeFactory scopeFactory)
    {
        _categoryId = categoryId;
        _userId = userId;
        _stateStorage = stateStorage;
        _scopeFactory = scopeFactory;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var newName = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newName) || newName.StartsWith('/'))
        {
            await bot.SendMessage(message.Chat.Id, "⚠️ Введите корректное название.\n\nДля отмены введите /cancel", cancellationToken: ct);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new UpdateCategoryCommand(_categoryId, newName, _userId), ct);
            _stateStorage.Clear(_userId);

            await bot.SendMessage(
                message.Chat.Id,
                $"✅ Категория переименована в <b>\"{newName}\"</b>.",
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