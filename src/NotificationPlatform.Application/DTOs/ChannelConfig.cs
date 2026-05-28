namespace NotificationPlatform.Application.DTOs;

public record ChannelConfig(
    string Type,
    Dictionary<string, string> Settings);
