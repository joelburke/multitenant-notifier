namespace NotificationPlatform.Domain.Exceptions;

public class RoutingRuleNotFoundException(Guid ruleId, Guid tenantId)
    : Exception($"Routing rule '{ruleId}' was not found for tenant '{tenantId}'.");
