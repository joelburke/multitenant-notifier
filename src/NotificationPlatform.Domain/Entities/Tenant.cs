namespace NotificationPlatform.Domain.Entities;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public int RateLimitPerMinute { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<RoutingRule> RoutingRules { get; private set; } = [];
    public ICollection<NotificationLog> NotificationLogs { get; private set; } = [];

    private Tenant() { }

    public static Tenant Create(string name, string slug, int rateLimitPerMinute)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            RateLimitPerMinute = rateLimitPerMinute,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, int rateLimitPerMinute)
    {
        Name = name;
        RateLimitPerMinute = rateLimitPerMinute;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
