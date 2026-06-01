namespace NotificationPlatform.Domain.Entities;

public class NotificationLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? RuleId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string ChannelType { get; private set; } = string.Empty;
    public DispatchStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string PayloadJson { get; private set; } = "{}";
    public DateTime CreatedAt { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Create(
        Guid tenantId,
        Guid? ruleId,
        string eventType,
        string channelType,
        DispatchStatus status,
        string payloadJson,
        string? errorMessage = null)
    {
        return new NotificationLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RuleId = ruleId,
            EventType = eventType,
            ChannelType = channelType,
            Status = status,
            PayloadJson = payloadJson,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum DispatchStatus
{
    Sent = 0,
    Failed = 1,
    RateLimited = 2
}
