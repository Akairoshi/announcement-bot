using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category, Guid>
    {
        Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<bool> ExistsAsync(string name, CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    }
}
