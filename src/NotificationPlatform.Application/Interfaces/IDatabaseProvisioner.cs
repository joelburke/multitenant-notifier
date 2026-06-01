namespace NotificationPlatform.Application.Interfaces;

/// <summary>
/// Handles the lifecycle of a tenant's isolated database.
/// Implemented in Infrastructure; called by TenantService at create/delete time.
/// </summary>
public interface IDatabaseProvisioner
{
    /// <summary>Creates the tenant database, runs migrations, returns its connection string.</summary>
    Task<string> ProvisionAsync(string tenantSlug, CancellationToken ct = default);

    /// <summary>Drops the tenant database. Irreversible — call only after removing the catalog record.</summary>
    Task DeprovisionAsync(string connectionString, CancellationToken ct = default);
}
