using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IAdminRequestRepository : IRepository<AdminRequest, Guid>
    {
        Task<IReadOnlyList<AdminRequest>> GetPendingAsync(int limit = 30, CancellationToken ct = default);
        Task<IReadOnlyList<AdminRequest>> GetByRequesterIdAsync(long requesterId, CancellationToken ct = default);
    }
}
