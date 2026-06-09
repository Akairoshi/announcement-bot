using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private readonly AppDbContext _context;

    public AnnouncementRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Announcement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Announcements.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Announcement>> GetByCategoryIdAsync(Guid categoryId, CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.CategoryId == categoryId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Announcement>> GetByAdminIdAsync(long adminId, CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.CreatedById == adminId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Announcement>> GetAllAsync(CancellationToken ct = default)
        => await _context.Announcements.ToListAsync(ct);

    public async Task<IReadOnlyList<Announcement>> GetOlderThanAsync(DateTime date, CancellationToken ct = default)
        => await _context.Announcements
            .Where(a => a.CreatedAt < date)
            .ToListAsync(ct);

    public async Task AddAsync(Announcement entity, CancellationToken ct = default)
        => await _context.Announcements.AddAsync(entity, ct);

    public Task UpdateAsync(Announcement entity, CancellationToken ct = default)
    {
        _context.Announcements.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Announcement entity, CancellationToken ct = default)
    {
        _context.Announcements.Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Announcement> entities, CancellationToken ct = default)
    {
        _context.Announcements.RemoveRange(entities);
        return Task.CompletedTask;
    }
}
