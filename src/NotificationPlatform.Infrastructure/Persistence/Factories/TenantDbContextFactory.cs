using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Infrastructure.Persistence.Factories;

/// <summary>
/// Resolves a tenant's connection string from the catalog (with a short-lived cache)
/// and creates an AppDbContext pointed at that tenant's isolated database.
/// </summary>
public class TenantDbContextFactory(CatalogDbContext catalog, IMemoryCache cache)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public virtual async Task<AppDbContext> CreateAsync(Guid tenantId, CancellationToken ct = default)
    {
        var cacheKey = $"tenant-connstr:{tenantId}";

        if (!cache.TryGetValue(cacheKey, out string? connectionString))
        {
            connectionString = await catalog.Tenants
                .Where(t => t.Id == tenantId && t.IsActive)
                .Select(t => t.ConnectionString)
                .FirstOrDefaultAsync(ct)
                ?? throw new TenantNotFoundException(tenantId);

            cache.Set(cacheKey, connectionString, CacheTtl);
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }

    public void InvalidateCache(Guid tenantId) =>
        cache.Remove($"tenant-connstr:{tenantId}");
}
