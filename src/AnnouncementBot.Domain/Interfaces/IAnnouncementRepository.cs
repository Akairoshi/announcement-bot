using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IAnnouncementRepository : IRepository<Announcement, Guid>
    {
        Task<IReadOnlyList<Announcement>> GetByCategoryIdAsync(Guid categoryId, CancellationToken ct = default);
        Task<IReadOnlyList<Announcement>> GetByAdminIdAsync(long adminId, CancellationToken ct = default);
        Task<IReadOnlyList<Announcement>> GetAllAsync(CancellationToken ct = default);
        Task<IReadOnlyList<Announcement>> GetOlderThanAsync(DateTime date, CancellationToken ct = default);
    }
}
