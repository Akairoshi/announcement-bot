namespace AnnouncementBot.Domain.Interfaces;

public interface IUnitOfWork
{
    IUserRepository Users { get; }
    ICategoryRepository Categories { get; }
    IAdminCategoryAccessRepository AdminCategoryAccesses { get; }
    ISubscriptionRepository Subscriptions { get; }
    ITemplateRepository Templates { get; }
    IAnnouncementRepository Announcements { get; }
    IDeliveryStatusRepository DeliveryStatuses { get; }
    IAdminRequestRepository AdminRequests { get; }
    IAuditLogRepository AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}