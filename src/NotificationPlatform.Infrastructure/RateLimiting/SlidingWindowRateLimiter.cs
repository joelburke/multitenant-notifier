using System.Collections.Concurrent;
using NotificationPlatform.Application.Interfaces;

namespace NotificationPlatform.Infrastructure.RateLimiting;

/// <summary>
/// Per-tenant sliding window rate limiter backed by in-memory state.
/// Each tenant's window is fully isolated — one tenant's load cannot affect another's counter.
///
/// Trade-off: state is not shared across instances (not suitable for horizontally-scaled deploys
/// without a distributed cache like Redis). Document this in the design doc.
/// </summary>
public class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<Guid, TenantWindow> _windows = new();
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    public bool TryConsume(Guid tenantId, int limitPerMinute)
    {
        var window = _windows.GetOrAdd(tenantId, _ => new TenantWindow());
        return window.TryConsume(limitPerMinute, WindowDuration);
    }

    private sealed class TenantWindow
    {
        private readonly Queue<DateTime> _timestamps = new();
        private readonly object _lock = new();

        public bool TryConsume(int limit, TimeSpan window)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - window;

            lock (_lock)
            {
                // Evict timestamps that have fallen outside the window
                while (_timestamps.Count > 0 && _timestamps.Peek() <= cutoff)
                    _timestamps.Dequeue();

                if (_timestamps.Count >= limit)
                    return false;

                _timestamps.Enqueue(now);
                return true;
            }
        }
    }
}
