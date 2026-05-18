using AnnouncementBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AnnouncementBot.Infrastructure.Persistence.Configurations
{
    public class AdminCategoryAccessConfiguration : IEntityTypeConfiguration<AdminCategoryAccess>
    {
        public void Configure(EntityTypeBuilder<AdminCategoryAccess> builder)
        {
            builder.ToTable("AdminCategoryAccesses");

            builder.HasKey(a => new { a.AdminId, a.CategoryId });

            builder.Property(a => a.AdminId);

            builder.HasOne<User>()
                .WithMany(u => u.AdminCategoryAccesses)
                .HasForeignKey(a => a.AdminId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Category>()
                .WithMany()
                .HasForeignKey(a => a.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
