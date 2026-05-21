
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Category?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Categories.FirstOrDefaultAsync(c => c.Name == name, ct);

    public async Task<bool> ExistsAsync(string name, CancellationToken ct = default)
        => await _context.Categories.AnyAsync(c => c.Name == name, ct);

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken ct = default)
        => await _context.Categories.ToListAsync(ct);

    public async Task AddAsync(Category entity, CancellationToken ct = default)
        => await _context.Categories.AddAsync(entity, ct);

    public Task UpdateAsync(Category entity, CancellationToken ct = default)
    {
        _context.Categories.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Category entity, CancellationToken ct = default)
    {
        _context.Categories.Remove(entity);
        return Task.CompletedTask;
    }
}