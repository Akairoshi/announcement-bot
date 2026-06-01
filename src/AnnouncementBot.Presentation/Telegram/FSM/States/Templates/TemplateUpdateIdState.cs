
using AnnouncementBot.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AnnouncementBot.Presentation.Telegram.FSM.States;

public class TemplateUpdateIdState : IConversationState
{
    private readonly long _userId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public TemplateUpdateIdState(long userId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        if (!Guid.TryParse(message.Text?.Trim(), out var templateId))
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

        _stateStorage.Set(_userId, new TemplateUpdateNameState(_userId, templateId, _scopeFactory, _stateStorage));

        await bot.SendMessage(
            message.Chat.Id,
            $"📌 Текущее название: {template.Name}\n\nВведите новое название (или /skip чтобы оставить):",
            cancellationToken: ct);
    }
}