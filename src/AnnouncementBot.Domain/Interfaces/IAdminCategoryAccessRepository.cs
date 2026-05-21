using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IAdminCategoryAccessRepository
    {
        Task<IReadOnlyList<AdminCategoryAccess>> GetByAdminIdAsync(long adminId, CancellationToken ct = default);
        Task AddAsync(AdminCategoryAccess entity, CancellationToken ct = default);
        Task DeleteAsync(AdminCategoryAccess entity, CancellationToken ct = default);
        Task<bool> ExistsAsync (long adminId, Guid categoryId, CancellationToken ct = default);
    }
}
