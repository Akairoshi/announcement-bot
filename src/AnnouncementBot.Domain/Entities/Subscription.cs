
namespace AnnouncementBot.Domain.Entities
{
    public class Subscription
    {
        public long UserId { get; private set; }
        public Guid CategoryId { get; private set; }
        public DateTime SubscribedAt {  get; private set; }

        private Subscription() { }

        public Subscription(long userId, Guid categoryId)
        {
            UserId = userId;
            CategoryId = categoryId;
            SubscribedAt = DateTime.UtcNow;
        }
    }
}
