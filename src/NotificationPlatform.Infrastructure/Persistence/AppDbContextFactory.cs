using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NotificationPlatform.Infrastructure.Persistence;

/// <summary>
/// Used by the EF Core CLI at design time only (migrations, scaffolding).
/// At runtime AppDbContext instances are created by TenantDbContextFactory with per-tenant connection strings.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=localhost,1433;Database=NotificationPlatform_DesignTime;User Id=sa;Password=Your_password123!;TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
