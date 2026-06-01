using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationPlatform.Infrastructure.Persistence.Factories;

namespace NotificationPlatform.Infrastructure.Persistence;

/// <summary>
/// On startup, runs any pending EF migrations against every active tenant database.
/// This ensures all tenant DBs stay in sync with the current schema after a deploy.
/// </summary>
public class TenantMigrationRunner(IServiceProvider services, ILogger<TenantMigrationRunner> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        // Migrate the catalog database itself first
        await catalog.Database.MigrateAsync(ct);

        var tenants = await catalog.Tenants
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.Slug, t.ConnectionString })
            .ToListAsync(ct);

        logger.LogInformation("Running migrations for {Count} tenant database(s).", tenants.Count);

        await Parallel.ForEachAsync(tenants, ct, async (tenant, token) =>
        {
            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlServer(tenant.ConnectionString,
                        sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
                    .Options;

                await using var db = new AppDbContext(options);
                await db.Database.MigrateAsync(token);

                logger.LogDebug("Migrated tenant database for '{Slug}'.", tenant.Slug);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed for tenant '{Slug}' ({Id}).", tenant.Slug, tenant.Id);
            }
        });
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
