using System.ComponentModel.DataAnnotations;

namespace NotificationPlatform.Application.DTOs;

public record IngestEventRequestDto(
    [Required] Guid TenantId,
    [Required, MinLength(1), MaxLength(200)] string EventType,
    Dictionary<string, object?>? Payload);

public record IngestEventResponseDto(
    int DispatchedCount,
    bool WasRateLimited,
    IList<string> MatchedChannels);

public record NotificationLogResponseDto(
    Guid Id,
    Guid TenantId,
    Guid? RuleId,
    string EventType,
    string ChannelType,
    string Status,
    string? ErrorMessage,
    DateTime CreatedAt);
