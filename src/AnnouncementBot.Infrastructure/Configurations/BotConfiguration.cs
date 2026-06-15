
namespace AnnouncementBot.Infrastructure.Configuration
{
    public sealed class BotConfiguration
    {
        public const string SectionName = "BotConfiguration";
        public string Token { get; init; } = string.Empty;
        public string SenderInterval { get; set; } = "3";
    }
}
