using AnnouncementBot.Domain.Entities;

namespace AnnouncementBot.Domain.Interfaces
{
    public interface IAuditLogRepository
    {
        Task AddAsync (AuditLog entity, CancellationToken ct = default);
    }
}
