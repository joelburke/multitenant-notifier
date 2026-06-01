using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Application.Services;
using NotificationPlatform.Infrastructure.Dispatchers;
using NotificationPlatform.Infrastructure.Persistence;
using NotificationPlatform.Infrastructure.Persistence.Factories;
using NotificationPlatform.Infrastructure.Persistence.Repositories;
using NotificationPlatform.Infrastructure.RateLimiting;

namespace NotificationPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Catalog DB — the one always-on shared database
        services.AddDbContext<CatalogDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("CatalogConnection"),
                sql => sql.MigrationsAssembly(typeof(CatalogDbContext).Assembly.FullName)));

        // Per-tenant context factory — scoped so it shares the CatalogDbContext within a request
        services.AddScoped<TenantDbContextFactory>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        services.AddScoped<IDatabaseProvisioner, DatabaseProvisioner>();

        services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();

        services.AddScoped<INotificationDispatcher, LogDispatcher>();
        services.AddScoped<INotificationDispatcher, WebhookDispatcher>();
        services.AddScoped<IDispatcherRegistry, DispatcherRegistry>();

        services.AddHttpClient("webhook").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

        services.AddMemoryCache();
        services.AddHostedService<TenantMigrationRunner>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<TenantService>();
        services.AddScoped<RoutingRuleService>();
        services.AddScoped<EventIngestionService>();
        return services;
    }
}
