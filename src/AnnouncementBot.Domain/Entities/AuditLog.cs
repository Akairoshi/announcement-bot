
namespace AnnouncementBot.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public long UserId { get; private set; }
        public string Action { get; private set; } = string.Empty;
        public string EntityName { get; private set; } = string.Empty;
        public string EntityId { get; private set; } = string.Empty;
        public string? Details { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private AuditLog() { }

        public AuditLog(long userId, string action,
            string entityName, string entityId, string? details)
        {
            UserId = userId;
            Action = action;
            EntityName = entityName;
            EntityId = entityId;
            Details = details;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
