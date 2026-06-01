using AnnouncementBot.Application.Commands.Announcements;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.FSM.States.Announcement;

public class AnnouncementConfirmState : IConversationState
{
    private readonly long _userId;
    private readonly string _text;
    private readonly Guid _categoryId;
    private readonly Guid? _templateId;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConversationStateStorage _stateStorage;

    public AnnouncementConfirmState(long userId, string text, Guid categoryId, Guid? templateId, IServiceScopeFactory scopeFactory, ConversationStateStorage stateStorage)
    {
        _userId = userId;
        _text = text;
        _categoryId = categoryId;
        _templateId = templateId;
        _scopeFactory = scopeFactory;
        _stateStorage = stateStorage;
    }

    public async Task HandleAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        await bot.SendMessage(message.Chat.Id, "⚠️ Нажмите кнопку выше.", cancellationToken: ct);
    }

    public async Task ConfirmAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new CreateAnnouncementCommand(
            _text, _categoryId, _userId, _templateId), ct);

        _stateStorage.Clear(_userId);

        await bot.SendMessage(chatId, "✅ Объявление создано и отправлено подписчикам.", cancellationToken: ct);

        // восстанавливаем клавиатуру
        using var scope2 = _scopeFactory.CreateScope();
        var unitOfWork = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var user = await unitOfWork.Users.GetByIdAsync(_userId, ct);

        var keyboard = user?.Role switch
        {
            UserRole.SuperAdmin => new ReplyKeyboardMarkup(new[]
            {
            new[] { new KeyboardButton("/profile"), new KeyboardButton("Лист объявлений") },
            new[] { new KeyboardButton("/make_announcement") },
            new[] { new KeyboardButton("/list_category"), new KeyboardButton("/add_category") },
            new[] { new KeyboardButton("/update_category"), new KeyboardButton("/remove_category") },
            new[] { new KeyboardButton("/list_admin"), new KeyboardButton("/remove_admin") },
            new[] { new KeyboardButton("/list_admin_request") },
            new[] { new KeyboardButton("/list_template"), new KeyboardButton("/add_template") },
            new[] { new KeyboardButton("/update_template"), new KeyboardButton("/remove_template") }
        })
            { ResizeKeyboard = true },

            _ => new ReplyKeyboardMarkup(new[]
            {
            new[] { new KeyboardButton("/profile"), new KeyboardButton("/list_announcement") },
            new[] { new KeyboardButton("/make_announcement") },
            new[] { new KeyboardButton("/list_template"), new KeyboardButton("/add_template") },
            new[] { new KeyboardButton("/update_template"), new KeyboardButton("/remove_template") }
        })
            { ResizeKeyboard = true }
        };

        await bot.SendMessage(
            chatId,
            "Выберите следующее действие:",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }
}