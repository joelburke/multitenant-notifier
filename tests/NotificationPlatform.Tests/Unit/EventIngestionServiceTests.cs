using FluentAssertions;
using Moq;
using NotificationPlatform.Application.DTOs;
using NotificationPlatform.Application.Interfaces;
using NotificationPlatform.Application.Services;
using NotificationPlatform.Domain.Entities;
using NotificationPlatform.Domain.Exceptions;

namespace NotificationPlatform.Tests.Unit;

public class EventIngestionServiceTests
{
    private readonly Mock<ITenantRepository> _tenantRepo = new();
    private readonly Mock<IRoutingRuleRepository> _ruleRepo = new();
    private readonly Mock<INotificationLogRepository> _logRepo = new();
    private readonly Mock<IDispatcherRegistry> _registry = new();
    private readonly Mock<IRateLimiter> _rateLimiter = new();

    private EventIngestionService CreateService() =>
        new(_tenantRepo.Object, _ruleRepo.Object, _logRepo.Object, _registry.Object, _rateLimiter.Object);

    [Fact]
    public async Task Throws_TenantNotFoundException_for_unknown_tenant()
    {
        _tenantRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((Tenant?)null);

        var svc = CreateService();
        var act = () => svc.IngestAsync(new IngestEventRequestDto(Guid.NewGuid(), "test.event", null));

        await act.Should().ThrowAsync<TenantNotFoundException>();
    }

    [Fact]
    public async Task Throws_RateLimitExceededException_when_limit_reached()
    {
        var tenant = Tenant.Create("Acme", "acme", 10);
        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, default)).ReturnsAsync(tenant);
        _rateLimiter.Setup(r => r.TryConsume(tenant.Id, 10)).Returns(false);
        _logRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), default)).Returns(Task.CompletedTask);
        _logRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var svc = CreateService();
        var act = () => svc.IngestAsync(new IngestEventRequestDto(tenant.Id, "test.event", null));

        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task Returns_zero_dispatched_when_no_rules_match()
    {
        var tenant = Tenant.Create("Acme", "acme", 100);
        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, default)).ReturnsAsync(tenant);
        _rateLimiter.Setup(r => r.TryConsume(tenant.Id, 100)).Returns(true);
        _ruleRepo.Setup(r => r.GetByTenantAsync(tenant.Id, default)).ReturnsAsync([]);
        _logRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var svc = CreateService();
        var result = await svc.IngestAsync(new IngestEventRequestDto(tenant.Id, "no.match", null));

        result.DispatchedCount.Should().Be(0);
        result.WasRateLimited.Should().BeFalse();
    }

    [Fact]
    public async Task Dispatches_to_matching_active_rules_only()
    {
        var tenant = Tenant.Create("Acme", "acme", 100);
        var channelsJson = """[{"type":"log","settings":{}}]""";
        var activeRule = RoutingRule.Create(tenant.Id, "Active", "user.signup", EventTypeMatchMode.Exact, channelsJson);
        var inactiveRule = RoutingRule.Create(tenant.Id, "Inactive", "user.signup", EventTypeMatchMode.Exact, channelsJson);
        inactiveRule.Deactivate();

        _tenantRepo.Setup(r => r.GetByIdAsync(tenant.Id, default)).ReturnsAsync(tenant);
        _rateLimiter.Setup(r => r.TryConsume(tenant.Id, 100)).Returns(true);
        _ruleRepo.Setup(r => r.GetByTenantAsync(tenant.Id, default)).ReturnsAsync([activeRule, inactiveRule]);

        var dispatcher = new Mock<INotificationDispatcher>();
        dispatcher.Setup(d => d.DispatchAsync(It.IsAny<DispatchRequestDto>(), default))
            .ReturnsAsync(DispatchResultDto.Ok());
        _registry.Setup(r => r.Resolve("log")).Returns(dispatcher.Object);
        _logRepo.Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), default)).Returns(Task.CompletedTask);
        _logRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var svc = CreateService();
        var result = await svc.IngestAsync(new IngestEventRequestDto(tenant.Id, "user.signup", null));

        result.DispatchedCount.Should().Be(1);
        dispatcher.Verify(d => d.DispatchAsync(It.IsAny<DispatchRequestDto>(), default), Times.Once);
    }

    [Fact]
    public async Task Tenant_A_rules_never_fire_for_Tenant_B_event()
    {
        var tenantA = Tenant.Create("Acme", "acme", 100);
        var tenantB = Tenant.Create("Globex", "globex", 100);
        var tenantARule = RoutingRule.Create(tenantA.Id, "Rule A", "alert", EventTypeMatchMode.Exact, """[{"type":"log","settings":{}}]""");

        _tenantRepo.Setup(r => r.GetByIdAsync(tenantB.Id, default)).ReturnsAsync(tenantB);
        _rateLimiter.Setup(r => r.TryConsume(tenantB.Id, 100)).Returns(true);

        _ruleRepo.Setup(r => r.GetByTenantAsync(tenantB.Id, default)).ReturnsAsync([]);
        _logRepo.Setup(r => r.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var svc = CreateService();
        var result = await svc.IngestAsync(new IngestEventRequestDto(tenantB.Id, "alert", null));

        result.DispatchedCount.Should().Be(0, "tenant B has no rules; tenant A's rules must not bleed across");
    }
}
