using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NotificationPlatform.Application.Interfaces;

namespace NotificationPlatform.Infrastructure.Persistence;

/// <summary>
/// Creates and drops SQL Server databases for tenants.
/// Uses the catalog connection string to reach the server, then switches to master for DDL.
/// </summary>
public class DatabaseProvisioner(IConfiguration configuration) : IDatabaseProvisioner
{
    public async Task<string> ProvisionAsync(string tenantSlug, CancellationToken ct = default)
    {
        var dbName = BuildDatabaseName(tenantSlug);
        var serverConnectionString = BuildServerConnectionString();
        var tenantConnectionString = BuildTenantConnectionString(dbName);

        await CreateDatabaseAsync(serverConnectionString, dbName, ct);
        await RunMigrationsAsync(tenantConnectionString, ct);

        return tenantConnectionString;
    }

    public async Task DeprovisionAsync(string connectionString, CancellationToken ct = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var dbName = builder.InitialCatalog;
        var serverConnectionString = BuildServerConnectionString();

        await using var connection = new SqlConnection(serverConnectionString);
        await connection.OpenAsync(ct);

        // Force disconnect active sessions before dropping
        var sql = $"""
            IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}')
            BEGIN
                ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{dbName}];
            END
            """;

        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string BuildDatabaseName(string slug) =>
        $"NotificationPlatform_{slug.Replace("-", "_")}";

    private string BuildServerConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(
            configuration.GetConnectionString("CatalogConnection")
            ?? throw new InvalidOperationException("CatalogConnection string is not configured."));

        builder.InitialCatalog = "master";
        return builder.ConnectionString;
    }

    private string BuildTenantConnectionString(string dbName)
    {
        var builder = new SqlConnectionStringBuilder(
            configuration.GetConnectionString("CatalogConnection")!);

        builder.InitialCatalog = dbName;
        return builder.ConnectionString;
    }

    private static async Task CreateDatabaseAsync(string serverConnectionString, string dbName, CancellationToken ct)
    {
        await using var connection = new SqlConnection(serverConnectionString);
        await connection.OpenAsync(ct);

        var sql = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}]";
        await using var cmd = new SqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task RunMigrationsAsync(string connectionString, CancellationToken ct)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.MigrateAsync(ct);
    }
}
