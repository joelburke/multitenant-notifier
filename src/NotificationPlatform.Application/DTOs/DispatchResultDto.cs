namespace NotificationPlatform.Application.DTOs;

public record DispatchResultDto(bool Success, string? ErrorMessage = null)
{
    public static DispatchResultDto Ok() => new(true);
    public static DispatchResultDto Fail(string error) => new(false, error);
}
