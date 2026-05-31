using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;

namespace NotificationPlatform.Infrastructure.Dispatchers;

public class WebhookDispatcher(IHttpClientFactory httpClientFactory, ILogger<WebhookDispatcher> logger) : INotificationDispatcher
{
    public string ChannelType => "webhook";

    public async Task<DispatchResultDto> DispatchAsync(DispatchRequestDto request, CancellationToken ct = default)
    {
        if (!request.Channel.Settings.TryGetValue("url", out var url) || string.IsNullOrWhiteSpace(url))
            return DispatchResultDto.Fail("Webhook channel is missing required 'url' setting.");

        var body = new
        {
            tenantId = request.TenantId,
            ruleId = request.RuleId,
            eventType = request.EventType,
            payload = request.Payload
        };

        try
        {
            var client = httpClientFactory.CreateClient("webhook");

            foreach (var (key, value) in request.Channel.Settings)
            {
                if (!key.Equals("url", StringComparison.OrdinalIgnoreCase))
                    client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
            }

            var response = await client.PostAsJsonAsync(url, body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("Webhook {Url} responded {Status}: {Body}", url, response.StatusCode, content);
                return DispatchResultDto.Fail($"Webhook returned {(int)response.StatusCode}.");
            }

            return DispatchResultDto.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Webhook dispatch failed for {Url}", url);
            return DispatchResultDto.Fail(ex.Message);
        }
    }
}
