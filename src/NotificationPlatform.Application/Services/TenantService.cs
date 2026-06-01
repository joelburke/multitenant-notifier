using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Application.Services;

public class TenantService(
    ITenantRepository tenantRepository,
    IDatabaseProvisioner provisioner)
{
    public async Task<IReadOnlyList<TenantResponseDto>> GetAllAsync(CancellationToken ct = default)
    {
        var tenants = await tenantRepository.GetAllAsync(ct);
        return tenants.Select(MapToResponse).ToList();
    }

    public async Task<TenantResponseDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(id, ct)
            ?? throw new TenantNotFoundException(id);
        return MapToResponse(tenant);
    }

    public async Task<TenantResponseDto> CreateAsync(CreateTenantRequestDto request, CancellationToken ct = default)
    {
        if (await tenantRepository.SlugExistsAsync(request.Slug, ct))
            throw new DuplicateTenantSlugException(request.Slug);

        // Provision the isolated database before writing the catalog record.
        // If provisioning fails the catalog record is never written — no orphan entry.
        var connectionString = await provisioner.ProvisionAsync(request.Slug, ct);

        var tenant = Tenant.Create(request.Name, request.Slug, request.RateLimitPerMinute, connectionString);
        await tenantRepository.AddAsync(tenant, ct);
        await tenantRepository.SaveChangesAsync(ct);
        return MapToResponse(tenant);
    }

    public async Task<TenantResponseDto> UpdateAsync(Guid id, UpdateTenantRequestDto request, CancellationToken ct = default)
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

        var connectionString = tenant.ConnectionString;

        // Remove catalog record first; provisioner drops the database after.
        await tenantRepository.DeleteAsync(tenant, ct);
        await tenantRepository.SaveChangesAsync(ct);

        await provisioner.DeprovisionAsync(connectionString, ct);
    }

    private static TenantResponseDto MapToResponse(Tenant t) =>
        new(t.Id, t.Name, t.Slug, t.RateLimitPerMinute, t.IsActive, t.CreatedAt);
}
