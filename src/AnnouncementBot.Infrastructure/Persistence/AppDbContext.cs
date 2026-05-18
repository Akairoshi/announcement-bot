using AnnouncementBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AnnouncementBot.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<AdminCategoryAccess> AdminCategoryAccesses => Set<AdminCategoryAccess>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<DeliveryStatus> DeliveryStatuses => Set<DeliveryStatus>();
    public DbSet<AdminRequest> AdminRequests => Set<AdminRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}