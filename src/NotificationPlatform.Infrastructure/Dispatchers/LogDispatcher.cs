using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;

namespace NotificationPlatform.Infrastructure.Dispatchers;

public class LogDispatcher(ILogger<LogDispatcher> logger) : INotificationDispatcher
{
    public string ChannelType => "log";

    public Task<DispatchResult> DispatchAsync(DispatchRequest request, CancellationToken ct = default)
    {
        logger.LogInformation(
            "NOTIFICATION | tenant={TenantId} rule={RuleId} event={EventType} payload={Payload}",
            request.TenantId,
            request.RuleId,
            request.EventType,
            JsonSerializer.Serialize(request.Payload));

        return Task.FromResult(DispatchResult.Ok());
    }
}
