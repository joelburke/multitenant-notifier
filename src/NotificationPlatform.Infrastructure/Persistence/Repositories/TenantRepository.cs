using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class TenantRepository(AppDbContext db) : ITenantRepository
{
    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        db.Tenants.OrderBy(t => t.Name).ToListAsync(ct)
            .ContinueWith<IReadOnlyList<Tenant>>(t => t.Result, ct);

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Slug == slug.ToLowerInvariant(), ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await db.Tenants.AddAsync(tenant, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public Task DeleteAsync(Tenant tenant, CancellationToken ct = default)
    {
        db.Tenants.Remove(tenant);
        return Task.CompletedTask;
    }
}
