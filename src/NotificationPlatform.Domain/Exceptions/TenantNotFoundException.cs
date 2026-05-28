namespace NotificationPlatform.Domain.Exceptions;

public class TenantNotFoundException(Guid tenantId)
    : Exception($"Tenant '{tenantId}' was not found.");
