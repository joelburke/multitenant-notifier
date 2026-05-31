using System.ComponentModel.DataAnnotations;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Application.DTOs;

public record RoutingRuleResponseDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string EventTypePattern,
    EventTypeMatchMode MatchMode,
    IList<ChannelConfigDto> Channels,
    int Priority,
    bool IsActive,
    DateTime CreatedAt);

public record CreateRoutingRuleRequestDto(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(1), MaxLength(200)] string EventTypePattern,
    EventTypeMatchMode MatchMode,
    [Required, MinLength(1)] IList<ChannelConfigDto> Channels,
    [Range(0, 1000)] int Priority = 0);

public record UpdateRoutingRuleRequestDto(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(1), MaxLength(200)] string EventTypePattern,
    EventTypeMatchMode MatchMode,
    [Required, MinLength(1)] IList<ChannelConfigDto> Channels,
    [Range(0, 1000)] int Priority);
