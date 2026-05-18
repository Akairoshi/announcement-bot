using AnnouncementBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnnouncementBot.Infrastructure.Persistence.Configurations;

public class AdminRequestConfiguration : IEntityTypeConfiguration<AdminRequest>
{
    public void Configure(EntityTypeBuilder<AdminRequest> builder)
    {
        builder.ToTable("AdminRequests");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(a => a.Details)
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.ReviewedAt)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.TargetId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.ReviewedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}