using System.Text.Json;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Application.Services;

public class EventIngestionService(
    ITenantRepository tenantRepository,
    IRoutingRuleRepository ruleRepository,
    INotificationLogRepository logRepository,
    IDispatcherRegistry dispatcherRegistry,
    IRateLimiter rateLimiter)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IngestEventResponseDto> IngestAsync(IngestEventRequestDto request, CancellationToken ct = default)
    {
        var tenant = await ValidateTenantAsync(request.TenantId, ct);
        await EnforceRateLimitAsync(tenant, request, ct);
        var matchingRules = await GetMatchingRulesAsync(tenant.Id, request.EventType, ct);
        var dispatchedChannels = await DispatchToChannelsAsync(tenant, matchingRules, request, ct);
        return new IngestEventResponseDto(dispatchedChannels.Count, false, dispatchedChannels);
    }

    private async Task<Tenant> ValidateTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new TenantNotFoundException(tenantId);

        if (!tenant.IsActive)
            throw new TenantNotFoundException(tenantId);

        return tenant;
    }

    private async Task EnforceRateLimitAsync(Tenant tenant, IngestEventRequestDto request, CancellationToken ct)
    {
        if (rateLimiter.TryConsume(tenant.Id, tenant.RateLimitPerMinute))
            return;

        var log = NotificationLog.Create(
            tenant.Id, null, request.EventType, "none",
            DispatchStatus.RateLimited,
            JsonSerializer.Serialize(request.Payload ?? [], JsonOptions));

        await logRepository.AddAsync(log, ct);
        throw new RateLimitExceededException(tenant.Id, tenant.RateLimitPerMinute);
    }

    private async Task<IReadOnlyList<RoutingRule>> GetMatchingRulesAsync(Guid tenantId, string eventType, CancellationToken ct)
    {
        var rules = await ruleRepository.GetByTenantAsync(tenantId, ct);
        return [.. rules.Where(r => r.IsActive && r.Matches(eventType)).OrderBy(r => r.Priority)];
    }

    private async Task<List<string>> DispatchToChannelsAsync(Tenant tenant, IReadOnlyList<RoutingRule> matchingRules, IngestEventRequestDto request, CancellationToken ct)
    {
        var payload = request.Payload ?? [];
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var dispatchedChannels = new List<string>();

        foreach (var rule in matchingRules)
        {
            var channels = JsonSerializer.Deserialize<IList<ChannelConfigDto>>(rule.ChannelsJson, JsonOptions) ?? [];
            foreach (var channel in channels)
            {
                var succeeded = await DispatchChannelAsync(tenant.Id, rule.Id, request.EventType, payload, payloadJson, channel, ct);
                if (succeeded) dispatchedChannels.Add(channel.Type);
            }
        }

        return dispatchedChannels;
    }

    private async Task<bool> DispatchChannelAsync(
        Guid tenantId, Guid ruleId, string eventType,
        Dictionary<string, object?> payload, string payloadJson,
        ChannelConfigDto channel, CancellationToken ct)
    {
        var dispatchRequest = new DispatchRequestDto(tenantId, ruleId, eventType, payload, channel);
        var dispatcher = dispatcherRegistry.Resolve(channel.Type);

        var result = dispatcher is null
            ? DispatchResultDto.Fail($"No dispatcher registered for channel type '{channel.Type}'.")
            : await dispatcher.DispatchAsync(dispatchRequest, ct);

        var status = result.Success ? DispatchStatus.Sent : DispatchStatus.Failed;
        var log = NotificationLog.Create(tenantId, ruleId, eventType, channel.Type, status, payloadJson, result.ErrorMessage);
        await logRepository.AddAsync(log, ct);

        return result.Success;
    }

    public async Task<IReadOnlyList<NotificationLogResponseDto>> GetLogsAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct = default)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct)
            ?? throw new TenantNotFoundException(tenantId);

        var logs = await logRepository.GetByTenantAsync(tenant.Id, page, pageSize, ct);
        return logs.Select(l => new NotificationLogResponseDto(
            l.Id, l.TenantId, l.RuleId, l.EventType,
            l.ChannelType, l.Status.ToString(), l.ErrorMessage, l.CreatedAt)).ToList();
    }
}
