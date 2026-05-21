
using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Subscription>> GetByUserIdAsync(long userId, CancellationToken ct = default)
        => await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Subscription>> GetByCategoryIdAsync(Guid categoryId, CancellationToken ct = default)
        => await _context.Subscriptions
            .Where(s => s.CategoryId == categoryId)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(long userId, Guid categoryId, CancellationToken ct = default)
        => await _context.Subscriptions
            .AnyAsync(s => s.UserId == userId && s.CategoryId == categoryId, ct);

    public async Task AddAsync(Subscription subscription, CancellationToken ct = default)
        => await _context.Subscriptions.AddAsync(subscription, ct);

    public Task DeleteAsync(Subscription subscription, CancellationToken ct = default)
    {
        _context.Subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }
}