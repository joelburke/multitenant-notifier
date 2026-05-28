using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence;
using NotificationPlatform.Infrastructure.Persistence.Repositories;

namespace NotificationPlatform.Tests.Integration;

/// <summary>
/// Verifies that the repository layer enforces tenant isolation at every query boundary.
/// Each test uses an isolated in-memory database to avoid cross-test interference.
/// </summary>
public class TenantIsolationTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Cannot_read_another_tenants_routing_rules()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Acme", "acme", 100);
        var tenantB = Tenant.Create("Globex", "globex", 100);
        db.Tenants.AddRange(tenantA, tenantB);

        var ruleA = RoutingRule.Create(tenantA.Id, "Rule A", "alert", EventTypeMatchMode.Exact, "[]");
        db.RoutingRules.Add(ruleA);
        await db.SaveChangesAsync();

        var repo = new RoutingRuleRepository(db);

        var rulesForB = await repo.GetByTenantAsync(tenantB.Id);
        rulesForB.Should().BeEmpty("tenant B must not see tenant A's rules");
    }

    [Fact]
    public async Task Cannot_fetch_another_tenants_rule_by_id()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Acme", "acme", 100);
        var tenantB = Tenant.Create("Globex", "globex", 100);
        db.Tenants.AddRange(tenantA, tenantB);

        var ruleA = RoutingRule.Create(tenantA.Id, "Rule A", "alert", EventTypeMatchMode.Exact, "[]");
        db.RoutingRules.Add(ruleA);
        await db.SaveChangesAsync();

        var repo = new RoutingRuleRepository(db);

        // Knowing rule A's ID and trying to fetch it as tenant B must return null
        var result = await repo.GetByIdAndTenantAsync(ruleA.Id, tenantB.Id);
        result.Should().BeNull("rule belongs to tenant A; tenant B cannot access it even with the correct ID");
    }

    [Fact]
    public async Task Cannot_read_another_tenants_notification_logs()
    {
        await using var db = CreateDb();
        var tenantA = Tenant.Create("Acme", "acme", 100);
        var tenantB = Tenant.Create("Globex", "globex", 100);
        db.Tenants.AddRange(tenantA, tenantB);

        var log = NotificationLog.Create(tenantA.Id, null, "alert", "log", DispatchStatus.Sent, "{}");
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync();

        var repo = new NotificationLogRepository(db);
        var logsForB = await repo.GetByTenantAsync(tenantB.Id, 1, 50);

        logsForB.Should().BeEmpty("tenant B must not see tenant A's notification logs");
    }

    [Fact]
    public async Task Deleting_tenant_cascades_to_its_rules_and_logs()
    {
        await using var db = CreateDb();
        var tenant = Tenant.Create("Acme", "acme", 100);
        db.Tenants.Add(tenant);

        var rule = RoutingRule.Create(tenant.Id, "Rule", "event", EventTypeMatchMode.Exact, "[]");
        var log = NotificationLog.Create(tenant.Id, null, "event", "log", DispatchStatus.Sent, "{}");
        db.RoutingRules.Add(rule);
        db.NotificationLogs.Add(log);
        await db.SaveChangesAsync();

        db.Tenants.Remove(tenant);
        await db.SaveChangesAsync();

        db.RoutingRules.Should().BeEmpty("rules must cascade-delete with tenant");
        db.NotificationLogs.Should().BeEmpty("logs must cascade-delete with tenant");
    }
}
