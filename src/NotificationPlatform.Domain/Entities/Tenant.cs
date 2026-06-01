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

    /// <summary>
    /// Connection string to this tenant's isolated database.
    /// Stored in the catalog DB only — never exposed through the API.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    private Tenant() { }

    public static Tenant Create(string name, string slug, int rateLimitPerMinute, string connectionString)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            RateLimitPerMinute = rateLimitPerMinute,
            ConnectionString = connectionString,
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
