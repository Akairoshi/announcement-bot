using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class MakeAnnouncementCommand : IBotCommand
{
    private readonly IServiceScopeFactory _scopeFactory;

    public MakeAnnouncementCommand(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public string Command => "/make_announcement";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user is null) return;

        List<Domain.Entities.Category> categories;

        if (user.Role == UserRole.SuperAdmin)
        {
            categories = (await unitOfWork.Categories.GetAllAsync(ct)).ToList();
        }
        else
        {
            var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);
            var categoryIds = accesses.Select(a => a.CategoryId).ToHashSet();
            var all = await unitOfWork.Categories.GetAllAsync(ct);
            categories = all.Where(c => categoryIds.Contains(c.Id)).ToList();
        }

        if (!categories.Any())
        {
            await bot.SendMessage(message.Chat.Id, "❌ Доступные категории отсутствуют.", cancellationToken: ct);
            return;
        }

        var buttons = categories
            .Select(c => new[] { InlineKeyboardButton.WithCallbackData($"📁 {c.Name}", $"ann_cat:{c.Id}") })
            .ToList();

        var inlineKeyboard = new InlineKeyboardMarkup(buttons);

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "📢 <b>Создание объявления</b>\n\nВыберите категорию:\n\nДля отмены введите /cancel",
            parseMode: ParseMode.Html,
            replyMarkup: inlineKeyboard,
            cancellationToken: ct);
    }
}