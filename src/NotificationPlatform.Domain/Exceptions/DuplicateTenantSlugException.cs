namespace NotificationPlatform.Domain.Exceptions;

public class DuplicateTenantSlugException(string slug)
    : Exception($"A tenant with slug '{slug}' already exists.");
