using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IDeliveryStatusRepository : IRepository<DeliveryStatus, Guid>
    {
        Task<IReadOnlyList<DeliveryStatus>> GetPendingOrFailedAsync(int maxRetryCount, CancellationToken ct = default);
        Task<IReadOnlyList<DeliveryStatus>> GetWithErrorCodeAsync(int errorCode, CancellationToken ct = default);
        Task<IReadOnlyList<DeliveryStatus>> GetByAnnouncementIdAsync(Guid announcementId, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<DeliveryStatus> statuses, CancellationToken ct = default);
    }
}
