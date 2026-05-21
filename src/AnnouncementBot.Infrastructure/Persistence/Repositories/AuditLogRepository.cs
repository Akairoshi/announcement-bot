
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly AppDbContext _context;
        public AuditLogRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(AuditLog entity, CancellationToken ct = default)
            => await _context.AddAsync(entity, ct);
    }
}
