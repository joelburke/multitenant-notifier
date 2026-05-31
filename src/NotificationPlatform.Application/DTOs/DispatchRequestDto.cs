namespace NotificationPlatform.Application.DTOs;

public record DispatchRequestDto(
    Guid TenantId,
    Guid RuleId,
    string EventType,
    Dictionary<string, object?> Payload,
    ChannelConfigDto Channel);
