using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Domain.Entities
{
    public class DeliveryStatus
    {
        public Guid Id { get; private set; }
        public Guid AnnouncementId { get; private set; }
        public long UserId { get; private set; }
        public DeliverySentStatus Status { get; private set; }
        public int RetryCount { get; private set; }
        public DateTime? LastAttemptAt { get; private set; }
        public DateTime? SentAt { get; private set; }

        private DeliveryStatus() { }

        public DeliveryStatus(Guid announcementId, long userId)
        {
            AnnouncementId = announcementId;
            UserId = userId;
            Status = DeliverySentStatus.Pending;
        }
        public void MarkAsSent()
        {
            Status = DeliverySentStatus.Sent;
            SentAt = DateTime.UtcNow;
            LastAttemptAt = DateTime.UtcNow;
        }

        public void MarkAsFailed()
        {
            RetryCount++;
            Status = DeliverySentStatus.Failed;
            LastAttemptAt = DateTime.UtcNow;
        }

    }
}
