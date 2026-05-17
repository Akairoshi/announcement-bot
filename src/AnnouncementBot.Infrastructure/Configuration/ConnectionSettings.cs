
namespace AnnouncementBot.Infrastructure.Configuration
{
    public sealed class ConnectionSettings
    {
        public const string SectionName = "ConnectionSettings";
        
        public string Host { get; init; } = string.Empty;
        public string Database { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        
        public string ToConnectionString()
        {
            return $"Host={Host};Database={Database};Username={UserName};Password={Password}";
        }
    }
}
