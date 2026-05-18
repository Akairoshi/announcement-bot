
using AnnouncementBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AAnnouncementBot.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever(); // ID приходит от Telegram, не генерируется БД

        builder.Property(u => u.UserName)
            .HasMaxLength(32) // лимит Telegram на username
            .IsRequired(false);

        builder.Property(u => u.Role)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(u => u.JoinedAt)
            .IsRequired();

        // backing fields для навигационных коллекций
        builder.Navigation(u => u.Subscriptions)
            .HasField("_subscriptions");

        builder.Navigation(u => u.AdminCategoryAccesses)
            .HasField("_adminCategoryAccesses");

        builder.Navigation(u => u.Templates)
            .HasField("_templates");

        builder.Navigation(u => u.Announcements)
            .HasField("_announcements");

        builder.Navigation(u => u.DeliveryStatuses)
            .HasField("_deliveryStatuses");

        builder.Navigation(u => u.AuditLogs)
            .HasField("_auditLogs");
    }
}