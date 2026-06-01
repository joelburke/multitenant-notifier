using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence.Configurations;

namespace NotificationPlatform.Infrastructure.Persistence;

/// <summary>
/// Per-tenant database — one instance per tenant, connected via a dynamically resolved
/// connection string. Contains only that tenant's routing rules and notification logs.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<RoutingRule> RoutingRules => Set<RoutingRule>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoutingRuleConfiguration());
        modelBuilder.ApplyConfiguration(new NotificationLogConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
