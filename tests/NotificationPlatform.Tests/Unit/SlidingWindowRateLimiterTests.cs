using FluentAssertions;
using NotificationPlatform.Infrastructure.RateLimiting;

namespace NotificationPlatform.Tests.Unit;

public class SlidingWindowRateLimiterTests
{
    private readonly SlidingWindowRateLimiter _limiter = new();

    [Fact]
    public void Allows_requests_within_limit()
    {
        var tenantId = Guid.NewGuid();
        for (var i = 0; i < 10; i++)
            _limiter.TryConsume(tenantId, 10).Should().BeTrue($"request {i + 1} should be allowed");
    }

    [Fact]
    public void Blocks_request_exceeding_limit()
    {
        var tenantId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
            _limiter.TryConsume(tenantId, 5);

        _limiter.TryConsume(tenantId, 5).Should().BeFalse("limit is exhausted");
    }

    [Fact]
    public void Tenant_A_limit_does_not_affect_Tenant_B()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Exhaust tenant A
        for (var i = 0; i < 3; i++)
            _limiter.TryConsume(tenantA, 3);

        _limiter.TryConsume(tenantA, 3).Should().BeFalse("tenant A is exhausted");

        // Tenant B must be completely unaffected
        _limiter.TryConsume(tenantB, 3).Should().BeTrue("tenant B has its own independent window");
    }

    [Fact]
    public void Different_limits_are_respected_per_tenant()
    {
        var highVolumetenant = Guid.NewGuid();
        var lowVolumeTenant = Guid.NewGuid();

        for (var i = 0; i < 100; i++)
            _limiter.TryConsume(highVolumetenant, 1000).Should().BeTrue();

        for (var i = 0; i < 2; i++)
            _limiter.TryConsume(lowVolumeTenant, 2);

        _limiter.TryConsume(lowVolumeTenant, 2).Should().BeFalse();
        _limiter.TryConsume(highVolumetenant, 1000).Should().BeTrue("high volume tenant still has headroom");
    }
}
