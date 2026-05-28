using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class RoutingRuleRepository(AppDbContext db) : IRoutingRuleRepository
{
    public Task<IReadOnlyList<RoutingRule>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        db.RoutingRules
            .Where(r => r.TenantId == tenantId)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<RoutingRule>>(t => t.Result, ct);

    public Task<RoutingRule?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        // TenantId filter here is the isolation gate — a rule ID alone is never sufficient
        db.RoutingRules.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantId, ct);

    public async Task AddAsync(RoutingRule rule, CancellationToken ct = default) =>
        await db.RoutingRules.AddAsync(rule, ct);

    public Task DeleteAsync(RoutingRule rule, CancellationToken ct = default)
    {
        db.RoutingRules.Remove(rule);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
