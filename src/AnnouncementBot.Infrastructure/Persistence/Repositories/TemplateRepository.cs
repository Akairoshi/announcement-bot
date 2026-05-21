
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly AppDbContext _context;

    public TemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Template?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Templates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Template>> GetByAdminIdAsync(long adminId, CancellationToken ct = default)
        => await _context.Templates
            .Where(t => t.CreatedById == adminId)
            .ToListAsync(ct);

    public async Task AddAsync(Template entity, CancellationToken ct = default)
        => await _context.Templates.AddAsync(entity, ct);

    public Task UpdateAsync(Template entity, CancellationToken ct = default)
    {
        _context.Templates.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Template entity, CancellationToken ct = default)
    {
        _context.Templates.Remove(entity);
        return Task.CompletedTask;
    }
}