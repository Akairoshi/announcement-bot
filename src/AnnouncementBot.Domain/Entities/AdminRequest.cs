
using AnnouncementBot.Domain.Enums;

namespace AnnouncementBot.Domain.Entities
{
    public class AdminRequest
    {
        public Guid Id { get; private set; }
        public long RequesterId { get; private set; }
        public long? TargetId { get; private set; } 
        public AdminRequestType Type { get; private set; }
        public string Details { get; private set; } = string.Empty;
        public AdminRequestStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? ReviewedAt { get; private set; }
        public long? ReviewedById { get; private set; }

        private AdminRequest() { }

        public AdminRequest(long requesterId, long? targetId, 
            AdminRequestType type, string details)
        {
            RequesterId = requesterId;
            TargetId = targetId;
            Type = type;
            Details = details;
            Status = AdminRequestStatus.Pending;
            CreatedAt = DateTime.UtcNow;
        }

        public void Approve(long reviewedById)
        {
            Status = AdminRequestStatus.Approved;
            ReviewedById = reviewedById;
            ReviewedAt = DateTime.UtcNow;
        }

        public void Reject(long reviewedById)
        {
            Status = AdminRequestStatus.Rejected;
            ReviewedById = reviewedById;
            ReviewedAt = DateTime.UtcNow;
        }

    }
}
