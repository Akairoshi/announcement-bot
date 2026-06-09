using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using AnnouncementBot.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence.Repositories;

public class DeliveryStatusRepository : IDeliveryStatusRepository
{
    private readonly AppDbContext _context;

    public DeliveryStatusRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DeliveryStatus?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DeliveryStatuses.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<IReadOnlyList<DeliveryStatus>> GetPendingOrFailedAsync(int maxRetryCount, CancellationToken ct = default)
        => await _context.DeliveryStatuses
            .Where(d => (d.Status == DeliverySentStatus.Pending || d.Status == DeliverySentStatus.Failed)
                && d.RetryCount < maxRetryCount)
            .ToListAsync(ct);
    public async Task<IReadOnlyList<DeliveryStatus>> GetWithErrorCodeAsync(int errorCode, CancellationToken ct = default) 
        => await _context.DeliveryStatuses
        .Where(d => ( d.ErrorStatus == (DeliveryErrorStatus)errorCode))
        .ToListAsync(ct);

    public async Task<IReadOnlyList<DeliveryStatus>> GetByAnnouncementIdAsync(Guid announcementId, CancellationToken ct = default)
        => await _context.DeliveryStatuses
            .Where(d => d.AnnouncementId == announcementId)
            .ToListAsync(ct);

    public async Task AddAsync(DeliveryStatus entity, CancellationToken ct = default)
        => await _context.DeliveryStatuses.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<DeliveryStatus> statuses, CancellationToken ct = default)
        => await _context.DeliveryStatuses.AddRangeAsync(statuses, ct);

    public Task UpdateAsync(DeliveryStatus entity, CancellationToken ct = default)
    {
        _context.DeliveryStatuses.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DeliveryStatus entity, CancellationToken ct = default)
    {
        _context.DeliveryStatuses.Remove(entity);
        return Task.CompletedTask;
    }
}