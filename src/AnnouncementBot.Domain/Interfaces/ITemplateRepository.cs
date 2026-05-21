
using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface ITemplateRepository : IRepository<Template, Guid>
    {
        Task<IReadOnlyList<Template>> GetByAdminIdAsync(long adminId, CancellationToken ct = default);
    }
}
