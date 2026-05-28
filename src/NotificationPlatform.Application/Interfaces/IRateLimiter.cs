namespace NotificationPlatform.Application.Interfaces;

public interface IRateLimiter
{
    /// <summary>
    /// Returns true if the request is allowed; false if the tenant is over their limit.
    /// </summary>
    bool TryConsume(Guid tenantId, int limitPerMinute);
}
