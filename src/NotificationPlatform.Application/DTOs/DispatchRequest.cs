namespace NotificationPlatform.Application.DTOs;

public record DispatchRequest(
    Guid TenantId,
    Guid RuleId,
    string EventType,
    Dictionary<string, object?> Payload,
    ChannelConfig Channel);
