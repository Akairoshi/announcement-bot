using AnnouncementBot.Presentation.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.Presentation.Telegram.Commands;

public class ListAnnouncementCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ListAnnouncementCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/list_announcement";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user is null) return;

        IReadOnlyList<Domain.Entities.Announcement> announcements;
        IReadOnlyList<Domain.Entities.Category> allCategories;

        if (user.Role == UserRole.SuperAdmin)
        {
            announcements = await unitOfWork.Announcements.GetAllAsync(ct);
            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        }
        else if (user.Role == UserRole.Admin)
        {
            var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);
            var categoryIds = accesses.Select(a => a.CategoryId).ToHashSet();

            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
            var allAnnouncements = await unitOfWork.Announcements.GetAllAsync(ct);
            announcements = allAnnouncements.Where(a => categoryIds.Contains(a.CategoryId)).ToList();
        }
        else
        {
            var subscriptions = await unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);
            var categoryIds = subscriptions.Select(s => s.CategoryId).ToHashSet();

            if (!categoryIds.Any())
            {
                await bot.SendMessage(
                    message.Chat.Id,
                    "📭 Вы не подписаны ни на одну категорию. Используйте /subscribe.",
                    cancellationToken: ct);
                return;
            }

            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
            var allAnnouncements = await unitOfWork.Announcements.GetAllAsync(ct);
            announcements = allAnnouncements.Where(a => categoryIds.Contains(a.CategoryId)).ToList();
        }

        var recent = announcements
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();

        if (!recent.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📢 Объявлений пока нет.",
                cancellationToken: ct);
            return;
        }

        var categoryMap = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var responseText = "<b>📬 Последние объявления:</b>\n\n";
        foreach (var ann in recent)
        {
            var categoryName = categoryMap.TryGetValue(ann.CategoryId, out var name) ? name : "Неизвестно";
            responseText += $"<b>📁</b> {categoryName}\n" +
                            $"<b>📅</b> {ann.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                            $"{ann.Text}\n" +
                            $"──────────────\n\n";
        }

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: responseText,
            parseMode: ParseMode.Html,
            cancellationToken: ct);
    }
}
