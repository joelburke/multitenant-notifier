using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Application.Services;
using NotificationPlatform.Infrastructure.Dispatchers;
using NotificationPlatform.Infrastructure.Persistence;
using NotificationPlatform.Infrastructure.Persistence.Repositories;
using NotificationPlatform.Infrastructure.RateLimiting;

namespace NotificationPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IRoutingRuleRepository, RoutingRuleRepository>();
        services.AddScoped<INotificationLogRepository, NotificationLogRepository>();

        services.AddSingleton<IRateLimiter, SlidingWindowRateLimiter>();

        // Dispatchers — each registered independently so new ones require only this list change
        services.AddScoped<INotificationDispatcher, LogDispatcher>();
        services.AddScoped<INotificationDispatcher, WebhookDispatcher>();
        services.AddScoped<IDispatcherRegistry, DispatcherRegistry>();

        services.AddHttpClient("webhook").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AllowAutoRedirect = false
        });

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
