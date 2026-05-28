namespace NotificationPlatform.Domain.Entities;

public class RoutingRule
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Pattern matched against the incoming event type (e.g. "user.signup", "order.*")
    /// </summary>
    public string EventTypePattern { get; private set; } = string.Empty;
    public EventTypeMatchMode MatchMode { get; private set; }

    /// <summary>
    /// JSON-serialised list of ChannelConfig objects.
    /// Stored as JSON to allow new channel types without schema migrations.
    /// </summary>
    public string ChannelsJson { get; private set; } = "[]";

    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    private RoutingRule() { }

    public static RoutingRule Create(
        Guid tenantId,
        string name,
        string eventTypePattern,
        EventTypeMatchMode matchMode,
        string channelsJson,
        int priority = 0)
    {
        return new RoutingRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            EventTypePattern = eventTypePattern,
            MatchMode = matchMode,
            ChannelsJson = channelsJson,
            Priority = priority,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string eventTypePattern, EventTypeMatchMode matchMode, string channelsJson, int priority)
    {
        Name = name;
        EventTypePattern = eventTypePattern;
        MatchMode = matchMode;
        ChannelsJson = channelsJson;
        Priority = priority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public bool Matches(string eventType) => MatchMode switch
    {
        EventTypeMatchMode.Exact    => string.Equals(EventTypePattern, eventType, StringComparison.OrdinalIgnoreCase),
        EventTypeMatchMode.Prefix   => eventType.StartsWith(EventTypePattern, StringComparison.OrdinalIgnoreCase),
        EventTypeMatchMode.Contains => eventType.Contains(EventTypePattern, StringComparison.OrdinalIgnoreCase),
        _                           => false
    };
}

public enum EventTypeMatchMode
{
    Exact = 0,
    Prefix = 1,
    Contains = 2
}
