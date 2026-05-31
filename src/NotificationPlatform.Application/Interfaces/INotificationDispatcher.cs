using NotificationPlatform.Application.DTOs;

namespace NotificationPlatform.Application.Interfaces;

/// <summary>
/// Implemented once per channel type (log, webhook, email, etc.).
/// The routing engine resolves the correct implementation via IDispatcherRegistry.
/// Adding a new channel requires only: implement this interface + register in DI.
/// </summary>
public interface INotificationDispatcher
{
    string ChannelType { get; }
    Task<DispatchResultDto> DispatchAsync(DispatchRequestDto request, CancellationToken ct = default);
}
