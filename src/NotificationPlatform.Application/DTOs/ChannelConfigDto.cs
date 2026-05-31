namespace NotificationPlatform.Application.DTOs;

public record ChannelConfigDto(
    string Type,
    Dictionary<string, string> Settings);
