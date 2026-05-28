using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Application.Interfaces;

public interface ITenantRepository
{
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
    Task DeleteAsync(Tenant tenant, CancellationToken ct = default);
}
