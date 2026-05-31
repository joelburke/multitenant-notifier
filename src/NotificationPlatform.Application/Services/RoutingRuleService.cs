using System.Text.Json;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Application.Services;

public class RoutingRuleService(
    IRoutingRuleRepository ruleRepository,
    ITenantRepository tenantRepository)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<RoutingRuleResponseDto>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);
        var rules = await ruleRepository.GetByTenantAsync(tenantId, ct);
        return rules.Select(MapToResponse).ToList();
    }

    public async Task<RoutingRuleResponseDto> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);
        var rule = await ruleRepository.GetByIdAndTenantAsync(id, tenantId, ct)
            ?? throw new RoutingRuleNotFoundException(id, tenantId);
        return MapToResponse(rule);
    }

    public async Task<RoutingRuleResponseDto> CreateAsync(Guid tenantId, CreateRoutingRuleRequestDto request, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);

        var channelsJson = JsonSerializer.Serialize(request.Channels, JsonOptions);
        var rule = RoutingRule.Create(tenantId, request.Name, request.EventTypePattern, request.MatchMode, channelsJson, request.Priority);

        await ruleRepository.AddAsync(rule, ct);
        await ruleRepository.SaveChangesAsync(ct);
        return MapToResponse(rule);
    }

    public async Task<RoutingRuleResponseDto> UpdateAsync(Guid id, Guid tenantId, UpdateRoutingRuleRequestDto request, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);

        var rule = await ruleRepository.GetByIdAndTenantAsync(id, tenantId, ct)
            ?? throw new RoutingRuleNotFoundException(id, tenantId);

        var channelsJson = JsonSerializer.Serialize(request.Channels, JsonOptions);
        rule.Update(request.Name, request.EventTypePattern, request.MatchMode, channelsJson, request.Priority);

        await ruleRepository.SaveChangesAsync(ct);
        return MapToResponse(rule);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        await EnsureTenantExistsAsync(tenantId, ct);

        var rule = await ruleRepository.GetByIdAndTenantAsync(id, tenantId, ct)
            ?? throw new RoutingRuleNotFoundException(id, tenantId);

        await ruleRepository.DeleteAsync(rule, ct);
        await ruleRepository.SaveChangesAsync(ct);
    }

    private async Task EnsureTenantExistsAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
        if (tenant == null) throw new TenantNotFoundException(tenantId);
    }

    private static RoutingRuleResponseDto MapToResponse(RoutingRule r)
    {
        var channels = JsonSerializer.Deserialize<IList<ChannelConfigDto>>(r.ChannelsJson, JsonOptions) ?? [];
        return new(r.Id, r.TenantId, r.Name, r.EventTypePattern, r.MatchMode, channels, r.Priority, r.IsActive, r.CreatedAt);
    }
}
