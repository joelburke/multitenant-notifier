using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Application.Services;

public class TenantService(ITenantRepository tenantRepository)
{
    public async Task<IReadOnlyList<TenantResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await tenantRepository.GetAllAsync(ct);
        return tenants.Select(MapToResponse).ToList();
    }

    public async Task<TenantResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(id, ct)
            ?? throw new TenantNotFoundException(id);
        return MapToResponse(tenant);
    }

    public async Task<TenantResponse> CreateAsync(CreateTenantRequest request, CancellationToken ct = default)
    {
        if (await tenantRepository.SlugExistsAsync(request.Slug, ct))
            throw new DuplicateTenantSlugException(request.Slug);

        var tenant = Tenant.Create(request.Name, request.Slug, request.RateLimitPerMinute);
        await tenantRepository.AddAsync(tenant, ct);
        await tenantRepository.SaveChangesAsync(ct);
        return MapToResponse(tenant);
    }

    public async Task<TenantResponse> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(id, ct)
            ?? throw new TenantNotFoundException(id);

        tenant.Update(request.Name, request.RateLimitPerMinute);
        await tenantRepository.SaveChangesAsync(ct);
        return MapToResponse(tenant);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(id, ct)
            ?? throw new TenantNotFoundException(id);

        await tenantRepository.DeleteAsync(tenant, ct);
        await tenantRepository.SaveChangesAsync(ct);
    }

    private static TenantResponse MapToResponse(Tenant t) =>
        new(t.Id, t.Name, t.Slug, t.RateLimitPerMinute, t.IsActive, t.CreatedAt);
}
