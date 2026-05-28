using System.ComponentModel.DataAnnotations;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Application.DTOs;

public record RoutingRuleResponse(
    Guid Id,
    Guid TenantId,
    string Name,
    string EventTypePattern,
    EventTypeMatchMode MatchMode,
    IList<ChannelConfig> Channels,
    int Priority,
    bool IsActive,
    DateTime CreatedAt);

public record CreateRoutingRuleRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(1), MaxLength(200)] string EventTypePattern,
    EventTypeMatchMode MatchMode,
    [Required, MinLength(1)] IList<ChannelConfig> Channels,
    [Range(0, 1000)] int Priority = 0);

public record UpdateRoutingRuleRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(1), MaxLength(200)] string EventTypePattern,
    EventTypeMatchMode MatchMode,
    [Required, MinLength(1)] IList<ChannelConfig> Channels,
    [Range(0, 1000)] int Priority);
