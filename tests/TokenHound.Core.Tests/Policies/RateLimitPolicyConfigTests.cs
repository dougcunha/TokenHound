using AwesomeAssertions;
using System;
using System.Threading.Tasks;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies dynamic configuration and safety floor clamping for <see cref="RateLimitPolicy"/>.
/// </summary>
public sealed class RateLimitPolicyConfigTests
{
    private static readonly DateTimeOffset BASE_TIME = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly RateLimitPolicy _policy = new();

    /// <summary>
    /// Verifies that the default effective retry floor is 60 seconds.
    /// </summary>
    [Fact]
    public void EffectiveRetryFloor_Default_ReturnsSixtySeconds()
    {

        _policy.EffectiveRetryFloor.Should().Be(RateLimitPolicy.MINIMUM_RETRY_FLOOR);
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that configuring floor values below 60 seconds clamps to the 60-second safety floor.
    /// </summary>
    [Fact]
    public void SetEffectiveFloor_WhenValueIsBelowSixtySeconds_ClampsToMinimumFloor()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(10));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        _policy.SetEffectiveFloor(TimeSpan.Zero);
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(-30));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(59));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that configuring valid floor values at or above 60 seconds updates the effective floor.
    /// </summary>
    [Fact]
    public void SetEffectiveFloor_WhenValueExceedsSixtySeconds_UpdatesEffectiveRetryFloor()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(120));

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(300));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Verifies that ResetEffectiveFloor restores the 60-second default floor.
    /// </summary>
    [Fact]
    public void ResetEffectiveFloor_RestoresMinimumSixtySecondFloor()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(180));
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(180));

        _policy.ResetEffectiveFloor();
        _policy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that a raised effective floor elevates jittered backoff calculations.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorRaised_EnforcesRaisedFloorOnJitteredBackoff()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadlineFullJitter = _policy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            1.0
        );

        var deadlineMinJitter = _policy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            0.01
        );

        deadlineFullJitter.Should().Be(BASE_TIME.AddSeconds(120));
        deadlineMinJitter.Should().Be(BASE_TIME.AddSeconds(120));
    }

    /// <summary>
    /// Verifies that a raised effective floor elevates Retry-After: 0 responses to the configured floor.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorRaised_EnforcesRaisedFloorOnRetryAfterZero()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = _policy.CalculateDeadline(
            BASE_TIME,
            0,
            1,
            1.0
        );

        deadline.Should().Be(BASE_TIME.AddSeconds(120));
        RateLimitPolicy.CanDispatch(BASE_TIME, deadline).Should().BeFalse();
        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(119), deadline).Should().BeFalse();
        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(120), deadline).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that server Retry-After exceeding the raised floor is preserved.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorRaised_AndRetryAfterExceedsFloor_PreservesServerHint()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = _policy.CalculateDeadline(
            BASE_TIME,
            180,
            1,
            1.0
        );

        deadline.Should().Be(BASE_TIME.AddSeconds(180));
    }

    /// <summary>
    /// Verifies that exponential backoff tiers exceeding the raised floor escalate monotonically.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorRaised_AndExponentialTierExceedsFloor_EscalatesMonotonically()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var fourthFailure = _policy.CalculateDeadline(
            BASE_TIME,
            null,
            4,
            0.0
        );

        fourthFailure.Should().Be(BASE_TIME.AddSeconds(240));
    }

    /// <summary>
    /// Verifies that the Random-based CalculateDeadline overload honors the effective retry floor.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WithRandomOverload_EnforcesRaisedFloor()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = _policy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            new Random(42)
        );

        deadline.Should().Be(BASE_TIME.AddSeconds(120));
    }

    /// <summary>
    /// Verifies that when a sub-floor value like 10 seconds is requested, 429 deadlines still enforce 60 seconds.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorClampedToSixtySeconds_EnforcesSixtySecondMinimum()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(10));

        var deadline = _policy.CalculateDeadline(
            BASE_TIME,
            0,
            1,
            1.0
        );

        deadline.Should().Be(BASE_TIME.AddSeconds(60));
    }

    /// <summary>
    /// Verifies that previously calculated deadlines and CanDispatch retain semantics when floor changes later.
    /// </summary>
    [Fact]
    public void CanDispatch_AndExistingRecordedDeadlines_RetainSemanticsWhenFloorChanges()
    {

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(60));

        var recordedDeadline = _policy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            1.0
        );

        recordedDeadline.Should().Be(BASE_TIME.AddSeconds(60));

        _policy.SetEffectiveFloor(TimeSpan.FromSeconds(300));

        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(59), recordedDeadline).Should().BeFalse();
        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(60), recordedDeadline).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that concurrent consumers retain independent retry floors.
    /// </summary>
    [Fact]
    public void ConcurrentPolicies_WithDifferentFloors_AreIsolated()
    {

        var defaultPolicy = new RateLimitPolicy(TimeSpan.FromSeconds(60));
        var raisedPolicy = new RateLimitPolicy(TimeSpan.FromSeconds(300));

        Parallel.Invoke(
            () => VerifyDeadline(defaultPolicy, 60),
            () => VerifyDeadline(raisedPolicy, 300)
        );
    }

    private static void VerifyDeadline(RateLimitPolicy policy, int expectedSeconds)
    {

        for (var i = 0; i < 100; i++)
        {
            var deadline = policy.CalculateDeadline(BASE_TIME, 0, 1, 1.0);

            deadline.Should().Be(BASE_TIME.AddSeconds(expectedSeconds));
        }
    }
}
