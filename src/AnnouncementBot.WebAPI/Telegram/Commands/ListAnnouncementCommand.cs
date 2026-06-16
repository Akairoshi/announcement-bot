using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AnnouncementBot.WebApi.Telegram.Commands;

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
        bool showStatistic = false;
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user is null) return;

        IReadOnlyList<Domain.Entities.Announcement> announcements;
        IReadOnlyList<Domain.Entities.Category> allCategories;

        if (user.Role == UserRole.SuperAdmin)
        {
            showStatistic = true;
            announcements = await unitOfWork.Announcements.GetAllAsync(ct);
            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        }
        else if (user.Role == UserRole.Admin)
        {
            showStatistic = true;
            var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);
            var categoryIds = accesses.Select(a => a.CategoryId).ToHashSet();

            var allAnnouncements = await unitOfWork.Announcements.GetAllAsync(ct);
            announcements = allAnnouncements.Where(a => a.CategoryId.HasValue && categoryIds.Contains(a.CategoryId.Value)).ToList();
            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        }
        else
        {
            announcements = await unitOfWork.Announcements.GetByAdminIdAsync(userId, ct);
            allCategories = await unitOfWork.Categories.GetAllAsync(ct);
        }

        var recent = announcements
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToList();

        if (!recent.Any())
        {
            await bot.SendMessage(
                message.Chat.Id,
                "📢 Объявления отсутствуют.",
                cancellationToken: ct);
            return;
        }

        var categoryMap = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var responseText = "📬 <b>Последние объявления:</b>\n\n";
        foreach (var ann in recent)
        {
            var categoryName = ann.CategoryId.HasValue && categoryMap.TryGetValue(ann.CategoryId.Value, out var name)
                ? name
                : "Удалённая категория";

            var successCount = ann.DeliveryStatuses.Count(x => x.Status == DeliverySentStatus.Sent);
            var failedCount = ann.DeliveryStatuses.Count(x => x.Status == DeliverySentStatus.Failed);
            var pendingCount = ann.DeliveryStatuses.Count(x => x.Status == DeliverySentStatus.Pending);

            responseText +=
                $"📁 {categoryName}\n" +
                $"📅 {ann.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                (showStatistic ? $"📊 <b>Статистика:</b>\n✅ {successCount} | ❌ {failedCount} | ⏳ {pendingCount}\n\n" : string.Empty) +
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