using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Domain.Entities
{
    public class User
    {
        public long Id { get; private set; }
        public string? UserName { get; private set; }
        public UserRole Role { get; private set; }
        public DateTime JoinedAt { get; private set; }

        private readonly List<Subscription> _subscriptions = [];
        private readonly List<AdminCategoryAccess> _adminCategoryAccesses = [];
        private readonly List<Template> _templates = [];
        private readonly List<Announcement> _announcements = [];
        private readonly List<DeliveryStatus> _deliveryStatuses = [];
        private readonly List<AuditLog> _auditLogs = [];

        public IReadOnlyCollection<Subscription> Subscriptions => _subscriptions.AsReadOnly();
        public IReadOnlyCollection<AdminCategoryAccess> AdminCategoryAccesses => _adminCategoryAccesses.AsReadOnly();
        public IReadOnlyCollection<Template> Templates => _templates.AsReadOnly();
        public IReadOnlyCollection<Announcement> Announcements => _announcements.AsReadOnly();
        public IReadOnlyCollection<DeliveryStatus> DeliveryStatuses => _deliveryStatuses.AsReadOnly();
        public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs.AsReadOnly();

        private User() { }

        public User(long id, string? userName)
        {
            Id = id;
            UserName = userName;
            Role = UserRole.User;
            JoinedAt = DateTime.UtcNow;
        }

        public void ChangeRole(UserRole role) => Role = role;
        public void UpdateUserName(string? userName) => UserName = userName;
    }
}
