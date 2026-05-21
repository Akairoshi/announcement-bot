using AnnouncementBot.Domain.Interfaces;
using AnnouncementBot.Infrastructure.Persistence.Repositories;

namespace AnnouncementBot.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IUserRepository Users { get; }
    public ICategoryRepository Categories { get; }
    public IAdminCategoryAccessRepository AdminCategoryAccesses { get; }
    public ISubscriptionRepository Subscriptions { get; }
    public ITemplateRepository Templates { get; }
    public IAnnouncementRepository Announcements { get; }
    public IDeliveryStatusRepository DeliveryStatuses { get; }
    public IAdminRequestRepository AdminRequests { get; }
    public IAuditLogRepository AuditLogs { get; }

    public UnitOfWork(AppDbContext context)
    {
        _context = context;

        Users = new UserRepository(context);
        Categories = new CategoryRepository(context);
        AdminCategoryAccesses = new AdminCategoryAccessRepository(context);
        Subscriptions = new SubscriptionRepository(context);
        Templates = new TemplateRepository(context);
        Announcements = new AnnouncementRepository(context);
        DeliveryStatuses = new DeliveryStatusRepository(context);
        AdminRequests = new AdminRequestRepository(context);
        AuditLogs = new AuditLogRepository(context);
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}