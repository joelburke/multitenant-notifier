namespace NotificationPlatform.Domain.Exceptions;

public class RateLimitExceededException(Guid tenantId, int limitPerMinute)
    : Exception($"Rate limit of {limitPerMinute} requests/minute exceeded for tenant '{tenantId}'.");
