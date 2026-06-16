using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.WebApi.Telegram.Commands.Interfaces;
using AnnouncementBot.WebApi.Telegram.Keyboards;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.WebApi.Telegram.Commands;

public class ProfileCommand : IBotCommand
{
    private readonly IServiceProvider _serviceProvider;

    public ProfileCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public string Command => "/profile";

    public async Task ExecuteAsync(ITelegramBotClient bot, Message message, CancellationToken ct)
    {
        var userId = message.From!.Id;

        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var user = await unitOfWork.Users.GetByIdAsync(userId, ct);
        if (user is null) return;

        ReplyKeyboardMarkup mainMenuKeyboard = ReplyKeyboards.GetMainKeyboard(user.Role);
        string roleName = user.Role switch
        {
            UserRole.User => "Пользователь 👤",
            UserRole.Admin => "Администратор ⚙️",
            UserRole.SuperAdmin => "Супер Администратор 👑",
            _ => "Неизвестно"
        };

        string plate;

        switch (user.Role)
        {
            case UserRole.SuperAdmin:
                {
                    var allCategories = await unitOfWork.Categories.GetAllAsync(ct);

                    var subscriptions = await unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);
                    var subscribedIds = subscriptions.Select(s => s.CategoryId).ToHashSet();
                    var subscribedCategories = allCategories.Where(c => subscribedIds.Contains(c.Id)).ToList();

                    string subscribeLabel = "📋 <b>Мои подписки:</b>";
                    string subscribeBlock = subscribedCategories.Any()
                        ? string.Join("\n", subscribedCategories.Select(c => $"  - {c.Name}"))
                        : "  Активные подписки отсутствуют";
                    plate = subscribeLabel + "\n" + subscribeBlock;
                    break;
                }
            case UserRole.Admin:
                {
                    var accesses = await unitOfWork.AdminCategoryAccesses.GetByAdminIdAsync(userId, ct);
                    var categoryIds = accesses.Select(a => a.CategoryId).ToHashSet();
                    var allCategories = await unitOfWork.Categories.GetAllAsync(ct);
                    var adminCategories = allCategories.Where(c => categoryIds.Contains(c.Id)).ToList();

                    string categoryLabel = "📂 <b>Мои категории:</b>";
                    string categoryBlock = adminCategories.Any()
                        ? string.Join("\n", adminCategories.Select(c => $"  - {c.Name}"))
                        : "  Назначенные категории отсутствуют";
                    var subscriptions = await unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);
                    var subscribedIds = subscriptions.Select(s => s.CategoryId).ToHashSet();
                    var subscribedCategories = allCategories.Where(c => subscribedIds.Contains(c.Id)).ToList();

                    string subscribeLabel = "📋 <b>Мои подписки:</b>";
                    string subscribeBlock = subscribedCategories.Any()
                        ? string.Join("\n", subscribedCategories.Select(c => $"  - {c.Name}"))
                        : "  Активные подписки отсутствуют";
                    plate = categoryLabel + "\n" + categoryBlock + "\n\n" + subscribeLabel + "\n" + subscribeBlock;
                    break;
                }
            default:
                {
                    var subscriptions = await unitOfWork.Subscriptions.GetByUserIdAsync(userId, ct);
                    var subscribedIds = subscriptions.Select(s => s.CategoryId).ToHashSet();
                    var allCategories = await unitOfWork.Categories.GetAllAsync(ct);
                    var subscribedCategories = allCategories.Where(c => subscribedIds.Contains(c.Id)).ToList();

                    string categoryLabel = "📋 <b>Мои подписки:</b>";
                    string categoryBlock = subscribedCategories.Any()
                        ? string.Join("\n", subscribedCategories.Select(c => $"  - {c.Name}"))
                        : "  Активные подписки отсутствуют";
                    plate = categoryLabel + "\n" + categoryBlock;
                    break;
                }
        }

        var response = $"👤 <b>Профиль пользователя</b>\n\n" +
                       $"<b>ID:</b> <code>{user.Id}</code>\n" +
                       $"<b>Аккаунт:</b> @{user.UserName ?? "отсутствует"}\n" +
                       $"<b>Роль:</b> {roleName}\n\n" +
                       $"{plate}";

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: response,
            parseMode: ParseMode.Html,
            replyMarkup: mainMenuKeyboard,
            cancellationToken: ct);
    }
}