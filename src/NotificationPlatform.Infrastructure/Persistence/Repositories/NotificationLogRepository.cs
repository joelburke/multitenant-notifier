using Microsoft.EntityFrameworkCore;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Infrastructure.Persistence.Repositories;

public class NotificationLogRepository(AppDbContext db) : INotificationLogRepository
{
    public Task<IReadOnlyList<NotificationLog>> GetByTenantAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default) =>
        db.NotificationLogs
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ContinueWith<IReadOnlyList<NotificationLog>>(t => t.Result, ct);

    public async Task AddAsync(NotificationLog log, CancellationToken ct = default) =>
        await db.NotificationLogs.AddAsync(log, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
