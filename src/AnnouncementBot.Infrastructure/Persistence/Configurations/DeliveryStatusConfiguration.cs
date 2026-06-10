using AnnouncementBot.Domain.Entities;
using AnnouncementBot.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnnouncementBot.Infrastructure.Persistence.Configurations;

public class DeliveryStatusConfiguration : IEntityTypeConfiguration<DeliveryStatus>
{
    public void Configure(EntityTypeBuilder<DeliveryStatus> builder)
    {
        builder.ToTable("DeliveryStatuses");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(d => d.ErrorStatus)
            .HasConversion<int>()
            .HasDefaultValue(DeliveryErrorStatus.None)
            .IsRequired();

        builder.Property(d => d.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.LastAttemptAt)
            .IsRequired(false);

        builder.Property(d => d.SentAt)
            .IsRequired(false);

        builder.HasOne<Announcement>()
            .WithMany(a => a.DeliveryStatuses)
            .HasForeignKey(d => d.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany(u => u.DeliveryStatuses)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
