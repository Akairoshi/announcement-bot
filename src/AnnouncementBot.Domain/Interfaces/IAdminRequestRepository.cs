
using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IAdminRequestRepository : IRepository<AdminRequest, Guid>
    {
        Task<IReadOnlyList<AdminRequest>> GetPendingAsync(CancellationToken ct = default);
        Task<IReadOnlyList<AdminRequest>> GetByRequesterIdAsync(long requesterId, CancellationToken ct = default);
    }
}
