using FluentAssertions;
using NotificationPlatform.Domain.Entities;

namespace NotificationPlatform.Tests.Unit;

public class RoutingRuleMatchTests
{
    private static RoutingRule MakeRule(string pattern, EventTypeMatchMode mode) =>
        RoutingRule.Create(Guid.NewGuid(), "test", pattern, mode, "[]");

    [Theory]
    [InlineData("user.signup", "user.signup", true)]
    [InlineData("user.signup", "user.login", false)]
    [InlineData("USER.SIGNUP", "user.signup", true)]
    public void Exact_matches_case_insensitively(string pattern, string eventType, bool expected)
    {
        var rule = MakeRule(pattern, EventTypeMatchMode.Exact);
        rule.Matches(eventType).Should().Be(expected);
    }

    [Theory]
    [InlineData("user.", "user.signup", true)]
    [InlineData("user.", "user.login", true)]
    [InlineData("user.", "order.placed", false)]
    [InlineData("USER.", "user.signup", true)]
    public void Prefix_matches_events_starting_with_pattern(string pattern, string eventType, bool expected)
    {
        var rule = MakeRule(pattern, EventTypeMatchMode.Prefix);
        rule.Matches(eventType).Should().Be(expected);
    }

    [Theory]
    [InlineData("signup", "user.signup", true)]
    [InlineData("signup", "org.signup.completed", true)]
    [InlineData("signup", "user.login", false)]
    [InlineData("SIGN", "user.signup", true)]
    public void Contains_matches_events_containing_pattern(string pattern, string eventType, bool expected)
    {
        var rule = MakeRule(pattern, EventTypeMatchMode.Contains);
        rule.Matches(eventType).Should().Be(expected);
    }

    [Fact]
    public void Inactive_rule_still_evaluates_match_logic()
    {
        // Inactive flag is checked by the service layer, not the entity — entity is pure logic
        var rule = RoutingRule.Create(Guid.NewGuid(), "test", "user.signup", EventTypeMatchMode.Exact, "[]");
        rule.Deactivate();
        rule.Matches("user.signup").Should().BeTrue();
    }
}
