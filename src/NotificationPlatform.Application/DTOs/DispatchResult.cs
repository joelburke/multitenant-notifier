namespace NotificationPlatform.Application.DTOs;

public record DispatchResult(bool Success, string? ErrorMessage = null)
{
    public static DispatchResult Ok() => new(true);
    public static DispatchResult Fail(string error) => new(false, error);
}
