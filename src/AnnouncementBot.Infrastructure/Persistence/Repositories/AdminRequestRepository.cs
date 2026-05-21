using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class AdminRequestRepository : IAdminRequestRepository
{
    private readonly AppDbContext _context;

    public AdminRequestRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.AdminRequests.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<AdminRequest>> GetPendingAsync(CancellationToken ct = default)
        => await _context.AdminRequests
            .Where(a => a.Status == AdminRequestStatus.Pending)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AdminRequest>> GetByRequesterIdAsync(long requesterId, CancellationToken ct = default)
        => await _context.AdminRequests
            .Where(a => a.RequesterId == requesterId)
            .ToListAsync(ct);

    public async Task AddAsync(AdminRequest entity, CancellationToken ct = default)
        => await _context.AdminRequests.AddAsync(entity, ct);

    public Task UpdateAsync(AdminRequest entity, CancellationToken ct = default)
    {
        _context.AdminRequests.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(AdminRequest entity, CancellationToken ct = default)
    {
        _context.AdminRequests.Remove(entity);
        return Task.CompletedTask;
    }
}