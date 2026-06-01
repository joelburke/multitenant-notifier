using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class TenantRepository(CatalogDbContext catalog) : ITenantRepository
{
    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        catalog.Tenants.OrderBy(t => t.Name).ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Tenant>>(t => t.Result, ct);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        catalog.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        catalog.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        catalog.Tenants.AnyAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await catalog.Tenants.AddAsync(tenant, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        catalog.SaveChangesAsync(ct);

    public Task DeleteAsync(Tenant tenant, CancellationToken ct = default)
    {
        catalog.Tenants.Remove(tenant);
        return Task.CompletedTask;
    }
}
