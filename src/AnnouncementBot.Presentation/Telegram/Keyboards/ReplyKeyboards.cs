using Telegram.Bot.Types.ReplyMarkups;

namespace AnnouncementBot.Presentation.Telegram.Keyboards;

public static class ReplyKeyboards
{
    public static ReplyKeyboardMarkup GetUserMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("/profile"), new KeyboardButton("/subscribe") },
            new[] { new KeyboardButton("/list_announcement"), new KeyboardButton("/admin_request") },
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }

    public static ReplyKeyboardMarkup GetAdminMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {

            new[] { new KeyboardButton("/profile"), new KeyboardButton("/subscribe"), new KeyboardButton("/admin_request") },
            new[] { new KeyboardButton("/make_announcement"), new KeyboardButton("/list_announcement") },
            new[] { new KeyboardButton("/add_template"), new KeyboardButton("/update_template"), new KeyboardButton("/remove_template") },
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }

    public static ReplyKeyboardMarkup GetSuperAdminMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("/profile"), new KeyboardButton("/subscribe") },
            new[] { new KeyboardButton("/make_announcement"), new KeyboardButton("/list_announcement") },

            new[] { new KeyboardButton("/list_category"), new KeyboardButton("/list_template"), new KeyboardButton("/list_admin_request")},
            new[] { new KeyboardButton("/update_template"), new KeyboardButton("/add_template"), new KeyboardButton("/remove_template") },
            new[] { new KeyboardButton("/update_category"), new KeyboardButton("/add_category"), new KeyboardButton("/remove_category") },
            new[] { new KeyboardButton("/list_admin"), new KeyboardButton("/list_admin_request"), new KeyboardButton("/remove_admin") },
            
        })
        {
            ResizeKeyboard = true,
            IsPersistent = true
        };
    }
}