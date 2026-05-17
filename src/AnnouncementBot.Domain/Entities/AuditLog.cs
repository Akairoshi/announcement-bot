
namespace AnnouncementBot.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; private set; }
        public long UserId { get; private set; }
        public int Action { get; private set; }
        public string EntityName { get; private set; } = string.Empty;
        public string EntityId { get; private set; }
        public string Details { get; private set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }

        private AuditLog() { }

        public AuditLog(long userId, int action,
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
