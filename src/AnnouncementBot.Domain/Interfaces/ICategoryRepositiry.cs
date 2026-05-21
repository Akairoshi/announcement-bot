using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface ICategoryRepository : IRepository<Category, long>
    {
        Task<Category?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<bool> ExistAsync(string name, CancellationToken ct = default);
        Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default);

    }
}
