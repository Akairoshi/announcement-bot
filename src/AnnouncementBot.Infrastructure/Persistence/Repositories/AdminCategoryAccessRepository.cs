using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class AdminCategoryAccessRepository : IAdminCategoryAccessRepository
{
    private readonly AppDbContext _context;

    public AdminCategoryAccessRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminCategoryAccess>> GetByAdminIdAsync(long adminId, CancellationToken ct = default)
        => await _context.AdminCategoryAccesses
            .Where(a => a.AdminId == adminId)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(long adminId, Guid categoryId, CancellationToken ct = default)
        => await _context.AdminCategoryAccesses
            .AnyAsync(a => a.AdminId == adminId && a.CategoryId == categoryId, ct);

    public async Task AddAsync(AdminCategoryAccess access, CancellationToken ct = default)
        => await _context.AdminCategoryAccesses.AddAsync(access, ct);

    public Task DeleteAsync(AdminCategoryAccess access, CancellationToken ct = default)
    {
        _context.AdminCategoryAccesses.Remove(access);
        return Task.CompletedTask;
    }
}