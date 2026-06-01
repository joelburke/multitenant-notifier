using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Infrastructure.Persistence.Factories;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class NotificationLogRepository(TenantDbContextFactory contextFactory) : INotificationLogRepository
{
    public async Task<IReadOnlyList<NotificationLog>> GetByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(tenantId, ct);
        return await db.NotificationLogs
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default)
    {
        await using var db = await contextFactory.CreateAsync(log.TenantId, ct);
        await db.NotificationLogs.AddAsync(log, ct);
        await db.SaveChangesAsync(ct);
    }
}
