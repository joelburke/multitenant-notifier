using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Application.Interfaces;

public interface IRoutingRuleRepository
{
    Task<IReadOnlyList<RoutingRule>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<RoutingRule?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task AddAsync(RoutingRule rule, CancellationToken ct = default);
    Task DeleteAsync(RoutingRule rule, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
