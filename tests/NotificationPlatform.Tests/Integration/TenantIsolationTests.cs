using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence;
using NotificationPlatform.Infrastructure.Persistence.Factories;
using NotificationPlatform.Infrastructure.Persistence.Repositories;

namespace NotificationPlatform.Tests.Integration;

/// <summary>
/// Verifies that the database-per-tenant pattern enforces isolation at every repository boundary.
///
/// Each tenant gets its own InMemory database (simulating a real separate SQL Server database).
/// The catalog holds the connection metadata. Isolation is structural — there is no shared
/// table that a WHERE clause could accidentally omit.
/// </summary>
public class TenantIsolationTests : IDisposable
{
    private readonly CatalogDbContext _catalog;
    private readonly IMemoryCache _cache;
    private readonly TenantDbContextFactory _factory;

    public TenantIsolationTests()
    {
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _catalog = new CatalogDbContext(catalogOptions);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _factory = new TestableTenantDbContextFactory(_catalog, _cache);
    }

    public void Dispose()
    {
        _catalog.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task Tenant_A_rules_are_not_visible_to_Tenant_B_repository()
    {
        var tenantA = await SeedTenantAsync("acme");
        var tenantB = await SeedTenantAsync("globex");

        // Write a rule directly into tenant A's database
        await using (var dbA = await _factory.CreateAsync(tenantA.Id))
        {
            var rule = RoutingRule.Create(tenantA.Id, "Rule A", "alert", EventTypeMatchMode.Exact, "[]");
            await dbA.RoutingRules.AddAsync(rule);
            await dbA.SaveChangesAsync();
        }

        // Tenant B's repository returns nothing — it connects to a completely separate database
        var repoB = new RoutingRuleRepository(_factory);
        var rulesForB = await repoB.GetByTenantAsync(tenantB.Id);

        rulesForB.Should().BeEmpty("tenant B's database is physically separate from tenant A's");
    }

    [Fact]
    public async Task Knowing_Tenant_A_rule_id_does_not_grant_Tenant_B_access()
    {
        var tenantA = await SeedTenantAsync("acme");
        var tenantB = await SeedTenantAsync("globex");

        Guid ruleAId;
        await using (var dbA = await _factory.CreateAsync(tenantA.Id))
        {
            var rule = RoutingRule.Create(tenantA.Id, "Rule A", "alert", EventTypeMatchMode.Exact, "[]");
            await dbA.RoutingRules.AddAsync(rule);
            await dbA.SaveChangesAsync();
            ruleAId = rule.Id;
        }

        // Tenant B tries to look up tenant A's rule by ID — returns null because the databases are separate
        var repoB = new RoutingRuleRepository(_factory);
        var result = await repoB.GetByIdAndTenantAsync(ruleAId, tenantB.Id);

        result.Should().BeNull("rule exists only in tenant A's database; tenant B's database has no rows at all");
    }

    [Fact]
    public async Task Tenant_A_logs_are_not_visible_to_Tenant_B_repository()
    {
        var tenantA = await SeedTenantAsync("acme");
        var tenantB = await SeedTenantAsync("globex");

        await using (var dbA = await _factory.CreateAsync(tenantA.Id))
        {
            var log = NotificationLog.Create(tenantA.Id, null, "alert", "log", DispatchStatus.Sent, "{}");
            await dbA.NotificationLogs.AddAsync(log);
            await dbA.SaveChangesAsync();
        }

        var repoB = new NotificationLogRepository(_factory);
        var logsForB = await repoB.GetByTenantAsync(tenantB.Id, 1, 50);

        logsForB.Should().BeEmpty("tenant B's database is physically separate from tenant A's");
    }

    [Fact]
    public async Task Each_tenant_only_sees_their_own_rules()
    {
        var tenantA = await SeedTenantAsync("acme");
        var tenantB = await SeedTenantAsync("globex");

        await using (var dbA = await _factory.CreateAsync(tenantA.Id))
        {
            await dbA.RoutingRules.AddAsync(RoutingRule.Create(tenantA.Id, "Rule A1", "event.a", EventTypeMatchMode.Exact, "[]"));
            await dbA.RoutingRules.AddAsync(RoutingRule.Create(tenantA.Id, "Rule A2", "event.b", EventTypeMatchMode.Exact, "[]"));
            await dbA.SaveChangesAsync();
        }

        await using (var dbB = await _factory.CreateAsync(tenantB.Id))
        {
            await dbB.RoutingRules.AddAsync(RoutingRule.Create(tenantB.Id, "Rule B1", "event.c", EventTypeMatchMode.Exact, "[]"));
            await dbB.SaveChangesAsync();
        }

        var repo = new RoutingRuleRepository(_factory);
        var rulesA = await repo.GetByTenantAsync(tenantA.Id);
        var rulesB = await repo.GetByTenantAsync(tenantB.Id);

        rulesA.Should().HaveCount(2, "tenant A has 2 rules in their own database");
        rulesB.Should().HaveCount(1, "tenant B has 1 rule in their own database");
        rulesA.Select(r => r.TenantId).Should().AllBeEquivalentTo(tenantA.Id);
        rulesB.Select(r => r.TenantId).Should().AllBeEquivalentTo(tenantB.Id);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private async Task<Tenant> SeedTenantAsync(string slug)
    {
        // Each tenant gets a unique InMemory database name as their "connection string"
        var tenant = Tenant.Create(slug, slug, 100, $"inmemory:{slug}-{Guid.NewGuid()}");
        await _catalog.Tenants.AddAsync(tenant);
        await _catalog.SaveChangesAsync();
        return tenant;
    }
}

/// <summary>
/// Test implementation of TenantDbContextFactory that uses InMemory databases
/// keyed by the connection string stored in the catalog.
/// </summary>
file sealed class TestableTenantDbContextFactory(CatalogDbContext catalog, IMemoryCache cache)
    : TenantDbContextFactory(catalog, cache)
{
    public override async Task<AppDbContext> CreateAsync(Guid tenantId, CancellationToken ct = default)
    {
        var connectionString = await catalog.Tenants
            .Where(t => t.Id == tenantId)
            .Select(t => t.ConnectionString)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found in catalog.");

        // Use the connection string value as the InMemory database name —
        // each tenant gets their own isolated in-memory store
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
