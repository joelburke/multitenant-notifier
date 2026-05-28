using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.HasMany(t => t.RoutingRules)
            .WithOne(r => r.Tenant)
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.NotificationLogs)
            .WithOne(l => l.Tenant)
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
