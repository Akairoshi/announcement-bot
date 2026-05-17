using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Domain.Entities
{
    public class User
    {
        public long Id { get; private set; }
        public string? UserName { get; private set; }
        public UserRole Role { get; private set; }
        public DateTime JoinedAt { get; private set; }

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
