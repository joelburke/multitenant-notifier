using NotificationPlatform.Application.Interfaces;

namespace NotificationPlatform.Infrastructure.Dispatchers;

/// <summary>
/// Resolves channel type strings to dispatcher implementations.
/// Open for extension: register new INotificationDispatcher implementations in DI
/// and they are automatically discovered here without changing this class.
/// </summary>
public class DispatcherRegistry(IEnumerable<INotificationDispatcher> dispatchers) : IDispatcherRegistry
{
    private readonly IReadOnlyDictionary<string, INotificationDispatcher> _map =
        dispatchers.ToDictionary(d => d.ChannelType, StringComparer.OrdinalIgnoreCase);

    public INotificationDispatcher? Resolve(string channelType) =>
        _map.TryGetValue(channelType, out var dispatcher) ? dispatcher : null;

    public IReadOnlyList<string> RegisteredChannelTypes => [.. _map.Keys];
}
