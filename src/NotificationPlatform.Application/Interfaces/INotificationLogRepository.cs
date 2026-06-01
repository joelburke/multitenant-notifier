using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Application.Interfaces;

public interface INotificationLogRepository
{
    Task<IReadOnlyList<NotificationLog>> GetByTenantAsync(
        Guid tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(NotificationLog log, CancellationToken ct = default);
}
