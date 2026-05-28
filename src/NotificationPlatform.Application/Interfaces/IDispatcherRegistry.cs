namespace NotificationPlatform.Application.Interfaces;

public interface IDispatcherRegistry
{
    INotificationDispatcher? Resolve(string channelType);
    IReadOnlyList<string> RegisteredChannelTypes { get; }
}
