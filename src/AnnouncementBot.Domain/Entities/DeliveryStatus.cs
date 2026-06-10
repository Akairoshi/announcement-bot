using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Domain.Entities
{
    public class DeliveryStatus
    {
        public Guid Id { get; private set; }
        public Guid AnnouncementId { get; private set; }
        public long UserId { get; private set; }
        public DeliverySentStatus Status { get; private set; }
        public DeliveryErrorStatus ErrorStatus { get; private set; }
        public int RetryCount { get; private set; }
        public DateTime? LastAttemptAt { get; private set; }
        public DateTime? SentAt { get; private set; }

        private DeliveryStatus() { }

        public DeliveryStatus(Guid announcementId, long userId)
        {
            AnnouncementId = announcementId;
            UserId = userId;
            Status = DeliverySentStatus.Pending;
            ErrorStatus = DeliveryErrorStatus.None;
        }

        public void MarkAsSent()
        {
            Status = DeliverySentStatus.Sent;
            ErrorStatus = DeliveryErrorStatus.None;
            SentAt = DateTime.UtcNow;
            LastAttemptAt = DateTime.UtcNow;
        }

        public void MarkAsFailed(DeliveryErrorStatus errorStatus)
        {
            ErrorStatus = errorStatus;
            LastAttemptAt = DateTime.UtcNow;

            if (errorStatus == DeliveryErrorStatus.NetworkError)
                return;

            RetryCount++;
            Status = DeliverySentStatus.Failed;
        }
    }
}
