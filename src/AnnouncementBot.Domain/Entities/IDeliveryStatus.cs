
using AnnouncementBot.Domain.Interfaces;

namespace AnnouncementBot.Domain.Entities
{
    public interface IDeliveryStatus : IRepository<DeliveryStatus, Guid>
    {
        Task<IReadOnlyList<DeliveryStatus>> GetFailedOrPendingAsync(int maxRetryCount, CancellationToken ct = default);
        Task<IReadOnlyList<DeliveryStatus>> GetByAnnouncementIdAsync(Guid announcementId, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<DeliveryStatus> statuses, CancellationToken ct = default);
    }
}
