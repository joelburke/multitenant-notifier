using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Configurations;

public class RoutingRuleConfiguration : IEntityTypeConfiguration<RoutingRule>
{
    public void Configure(EntityTypeBuilder<RoutingRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);
        builder.Property(r => r.EventTypePattern).IsRequired().HasMaxLength(200);
        builder.Property(r => r.MatchMode).IsRequired();
        builder.Property(r => r.ChannelsJson).IsRequired().HasColumnType("nvarchar(max)");

        // Filtering by TenantId is the critical isolation boundary — index makes it fast.
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => new { r.TenantId, r.IsActive });
    }
}
