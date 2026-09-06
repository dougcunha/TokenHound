using AwesomeAssertions;
using System;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies rate-limit dispatch evaluation and deadline calculation in <see cref="RateLimitPolicy"/>.
/// </summary>
public sealed class RateLimitPolicyTests
{
    private static readonly DateTimeOffset BASE_TIME = new(2026, 9, 6, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that CanDispatch allows dispatch when deadline is null.
    /// </summary>
    [Fact]
    public void CanDispatch_WhenDeadlineIsNull_ReturnsTrue()
    {
        var result = RateLimitPolicy.CanDispatch(BASE_TIME, null);

        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanDispatch returns false when current time is strictly before the deadline.
    /// </summary>
    [Fact]
    public void CanDispatch_WhenNowIsBeforeDeadline_ReturnsFalse()
    {
        var deadline = BASE_TIME.AddSeconds(30);

        var result = RateLimitPolicy.CanDispatch(BASE_TIME, deadline);

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CanDispatch returns true once current time reaches or surpasses the deadline.
    /// </summary>
    [Fact]
    public void CanDispatch_WhenNowIsAtOrPastDeadline_ReturnsTrue()
    {
        var exactDeadline = BASE_TIME;
        var pastDeadline = BASE_TIME.AddSeconds(-1);

        var atDeadlineResult = RateLimitPolicy.CanDispatch(BASE_TIME, exactDeadline);
        var pastDeadlineResult = RateLimitPolicy.CanDispatch(BASE_TIME, pastDeadline);

        atDeadlineResult.Should().BeTrue();
        pastDeadlineResult.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that Retry-After values less than 60 seconds (including 0 and negatives) are elevated to the 60-second minimum floor.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenRetryAfterIsBelowSixty_EnforcesSixtySecondFloor()
    {
        var deadlineZero = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            0,
            1,
            1.0
        );

        var deadlineNegative = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            -15,
            1,
            1.0
        );

        var deadlineThirty = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            30,
            1,
            1.0
        );

        deadlineZero.Should().Be(BASE_TIME.AddSeconds(60));
        deadlineNegative.Should().Be(BASE_TIME.AddSeconds(60));
        deadlineThirty.Should().Be(BASE_TIME.AddSeconds(60));
    }

    /// <summary>
    /// Verifies that Retry-After values exceeding 60 seconds are preserved exactly.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenRetryAfterExceedsSixty_PreservesProvidedSeconds()
    {
        var deadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            180,
            1,
            1.0
        );

        deadline.Should().Be(BASE_TIME.AddSeconds(180));
    }

    /// <summary>
    /// Verifies that when Retry-After is null, deadline is computed using BackoffCalculator.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenRetryAfterIsNull_UsesBackoffCalculator()
    {
        var zeroFailuresDeadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            0,
            1.0
        );

        var oneFailureDeadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            1.0
        );

        var fiveFailuresDeadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            5,
            1.0
        );

        zeroFailuresDeadline.Should().Be(BASE_TIME);
        oneFailureDeadline.Should().Be(BASE_TIME.AddSeconds(60));
        fiveFailuresDeadline.Should().Be(BASE_TIME.AddSeconds(960));
    }

    /// <summary>
    /// Verifies that CalculateDeadline with random generator produces a deadline within expected jitter bounds.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WithRandom_GeneratesBoundedDeadline()
    {
        var random = new Random(42);

        var deadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            random
        );

        deadline.Should().BeOnOrAfter(BASE_TIME);
        deadline.Should().BeOnOrBefore(BASE_TIME.AddSeconds(60));
    }

    /// <summary>
    /// Verifies minimum retry floor constant.
    /// </summary>
    [Fact]
    public void RateLimitPolicy_Constants_MatchExpected()
    {
        RateLimitPolicy.MINIMUM_RETRY_FLOOR.Should().Be(TimeSpan.FromSeconds(60));
    }
}
