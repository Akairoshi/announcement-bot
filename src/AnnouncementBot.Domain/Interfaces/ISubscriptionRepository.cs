using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<IReadOnlyList<Subscription>> GetByUserIdAsync(long userId, CancellationToken ct = default);
        Task<IReadOnlyList<Subscription>> GetByCategoryIdAsync(Guid categoryId, CancellationToken ct = default);
        Task AddAsync(Subscription entity, CancellationToken ct = default);
        Task DeleteAsync(Subscription entity, CancellationToken ct = default);
        Task<bool> ExistsAsync (long userId, Guid categoryId, CancellationToken ct = default);
    }
}
