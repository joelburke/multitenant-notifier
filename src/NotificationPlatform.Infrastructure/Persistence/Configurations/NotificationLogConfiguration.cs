using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.EventType).IsRequired().HasMaxLength(200);
        builder.Property(l => l.ChannelType).IsRequired().HasMaxLength(50);
        builder.Property(l => l.PayloadJson).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(l => l.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => new { l.TenantId, l.CreatedAt });
    }
}
