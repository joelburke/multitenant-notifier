using System.ComponentModel.DataAnnotations;

namespace NotificationPlatform.Application.DTOs;

public record TenantResponse(
    Guid Id,
    string Name,
    string Slug,
    int RateLimitPerMinute,
    bool IsActive,
    DateTime CreatedAt);

public record CreateTenantRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Required, MinLength(2), MaxLength(50), RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must be lowercase alphanumeric with hyphens only.")] string Slug,
    [Range(1, 10000)] int RateLimitPerMinute = 100);

public record UpdateTenantRequest(
    [Required, MinLength(2), MaxLength(100)] string Name,
    [Range(1, 10000)] int RateLimitPerMinute);
