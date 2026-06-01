using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence.Factories;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class RoutingRuleRepository(TenantDbContextFactory contextFactory) : IRoutingRuleRepository
{
    public async Task<IReadOnlyList<RoutingRule>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(tenantId, ct);
        return await db.RoutingRules
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<RoutingRule?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(tenantId, ct);
        // The database IS the tenant boundary; TenantId filter retained as secondary guard.
        return await db.RoutingRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);
    }

    public async Task AddAsync(RoutingRule rule, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(rule.TenantId, ct);
        await db.RoutingRules.AddAsync(rule, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(RoutingRule rule, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(rule.TenantId, ct);
        db.RoutingRules.Update(rule);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(RoutingRule rule, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(rule.TenantId, ct);
        db.RoutingRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }
}
