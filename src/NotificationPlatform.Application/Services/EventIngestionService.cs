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
        var tenant = await GetAndValidateTenantAsync(request.TenantId, ct);
        await EnforceRateLimitOrThrowAsync(tenant, request.EventType, request.Payload, ct);
        return await DispatchEventToMatchingRulesAsync(tenant, request, ct);
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

    private async Task<Tenant> GetAndValidateTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant is null || !tenant.IsActive)
            throw new TenantNotFoundException(tenantId);
        return tenant;
    }

    private async Task EnforceRateLimitOrThrowAsync(
        Tenant tenant, string eventType, Dictionary<string, object?>? payload, CancellationToken ct)
    {
        if (rateLimiter.TryConsume(tenant.Id, tenant.RateLimitPerMinute))
            return;

        var rateLimitedLog = NotificationLog.Create(
            tenant.Id, null, eventType, "none",
            DispatchStatus.RateLimited, SerializePayload(payload));
        await logRepository.AddAsync(rateLimitedLog, ct);
        await logRepository.SaveChangesAsync(ct);

        throw new RateLimitExceededException(tenant.Id, tenant.RateLimitPerMinute);
    }

    private async Task<IngestEventResponseDto> DispatchEventToMatchingRulesAsync(
        Tenant tenant, IngestEventRequestDto request, CancellationToken ct)
    {
        var matchingRules = await GetActiveRulesMatchingEventTypeAsync(tenant.Id, request.EventType, ct);
        var payload = request.Payload ?? new();
        var payloadJson = SerializePayload(payload);
        var successfulChannels = new List<string>();

        foreach (var rule in matchingRules)
        {
            foreach (var channel in DeserializeChannels(rule.ChannelsJson))
            {
                var result = await DispatchToChannelAsync(tenant.Id, rule.Id, request.EventType, payload, channel, ct);
                await LogDispatchResultAsync(tenant.Id, rule.Id, request.EventType, channel.Type, payloadJson, result, ct);

                if (result.Success)
                    successfulChannels.Add(channel.Type);
            }
        }

        await logRepository.SaveChangesAsync(ct);
        return new IngestEventResponseDto(successfulChannels.Count, false, successfulChannels);
    }

    private async Task<IEnumerable<RoutingRule>> GetActiveRulesMatchingEventTypeAsync(
        Guid tenantId, string eventType, CancellationToken ct)
    {
        var tenantRules = await ruleRepository.GetByTenantAsync(tenantId, ct);
        return tenantRules
            .Where(rule => rule.IsActive && rule.Matches(eventType))
            .OrderBy(rule => rule.Priority);
    }

    private async Task<DispatchResultDto> DispatchToChannelAsync(
        Guid tenantId, Guid ruleId, string eventType,
        Dictionary<string, object?> payload, ChannelConfigDto channel, CancellationToken ct)
    {
        var dispatcher = dispatcherRegistry.Resolve(channel.Type);
        if (dispatcher is null)
            return DispatchResultDto.Fail($"No dispatcher registered for channel type '{channel.Type}'.");

        var dispatchRequest = new DispatchRequestDto(tenantId, ruleId, eventType, payload, channel);
        return await dispatcher.DispatchAsync(dispatchRequest, ct);
    }

    private async Task LogDispatchResultAsync(
        Guid tenantId, Guid ruleId, string eventType, string channelType,
        string payloadJson, DispatchResultDto result, CancellationToken ct)
    {
        var status = result.Success ? DispatchStatus.Sent : DispatchStatus.Failed;
        var log = NotificationLog.Create(tenantId, ruleId, eventType, channelType, status, payloadJson, result.ErrorMessage);
        await logRepository.AddAsync(log, ct);
    }

    private static string SerializePayload(Dictionary<string, object?>? payload) =>
        JsonSerializer.Serialize(payload ?? new(), JsonOptions);

    private static IList<ChannelConfigDto> DeserializeChannels(string channelsJson) =>
        JsonSerializer.Deserialize<IList<ChannelConfigDto>>(channelsJson, JsonOptions) ?? [];
}
