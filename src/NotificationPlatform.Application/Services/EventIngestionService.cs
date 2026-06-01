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
        var tenant = await tenantRepository.GetByIdAsync(request.TenantId, ct)
            ?? throw new TenantNotFoundException(request.TenantId);

        if (!tenant.IsActive)
            throw new TenantNotFoundException(request.TenantId);

        if (!rateLimiter.TryConsume(tenant.Id, tenant.RateLimitPerMinute))
        {
            var rateLimitLog = NotificationLog.Create(
                tenant.Id, null, request.EventType, "none",
                DispatchStatus.RateLimited,
                JsonSerializer.Serialize(request.Payload ?? [], JsonOptions));

            await logRepository.AddAsync(rateLimitLog, ct);
            throw new RateLimitExceededException(tenant.Id, tenant.RateLimitPerMinute);
        }

        var rules = await ruleRepository.GetByTenantAsync(tenant.Id, ct);
        var matchingRules = rules
            .Where(r => r.IsActive && r.Matches(request.EventType))
            .OrderBy(r => r.Priority)
            .ToList();

        var payload = request.Payload ?? [];
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var matchedChannels = new List<string>();

        foreach (var rule in matchingRules)
        {
            var channels = JsonSerializer.Deserialize<IList<ChannelConfigDto>>(rule.ChannelsJson, JsonOptions) ?? [];

            foreach (var channel in channels)
            {
                var dispatchRequest = new DispatchRequestDto(tenant.Id, rule.Id, request.EventType, payload, channel);
                var dispatcher = dispatcherRegistry.Resolve(channel.Type);

                DispatchResultDto result = dispatcher is null
                    ? DispatchResultDto.Fail($"No dispatcher registered for channel type '{channel.Type}'.")
                    : await dispatcher.DispatchAsync(dispatchRequest, ct);

                var status = result.Success ? DispatchStatus.Sent : DispatchStatus.Failed;
                var log = NotificationLog.Create(tenant.Id, rule.Id, request.EventType, channel.Type, status, payloadJson, result.ErrorMessage);
                await logRepository.AddAsync(log, ct);

                if (result.Success) matchedChannels.Add(channel.Type);
            }
        }

        return new IngestEventResponseDto(matchedChannels.Count, false, matchedChannels);
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
