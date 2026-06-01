using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence.Configurations;

namespace NotificationPlatform.Infrastructure.Persistence;

/// <summary>
/// The catalog database — always available, stores tenant registry including each tenant's
/// connection string. This is the only shared database in the system.
/// </summary>
public class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantCatalogConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
