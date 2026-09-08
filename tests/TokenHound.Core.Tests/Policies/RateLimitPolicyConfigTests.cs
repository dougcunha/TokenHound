using AwesomeAssertions;
using System;
using System.Threading.Tasks;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies dynamic configuration and safety floor clamping for <see cref="RateLimitPolicy"/>.
/// </summary>
public sealed class RateLimitPolicyConfigTests : IDisposable
{
    private static readonly DateTimeOffset BASE_TIME = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitPolicyConfigTests"/> with clean policy state.
    /// </summary>
    public RateLimitPolicyConfigTests()
    {

        RateLimitPolicy.ResetEffectiveFloor();
    }

    /// <inheritdoc />
    public void Dispose()
    {

        RateLimitPolicy.ResetEffectiveFloor();
    }

    /// <summary>
    /// Verifies that the default effective retry floor is 60 seconds.
    /// </summary>
    [Fact]
    public void EffectiveRetryFloor_Default_ReturnsSixtySeconds()
    {

        RateLimitPolicy.EffectiveRetryFloor.Should().Be(RateLimitPolicy.MINIMUM_RETRY_FLOOR);
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that configuring floor values below 60 seconds clamps to the 60-second safety floor.
    /// </summary>
    [Fact]
    public void SetEffectiveFloor_WhenValueIsBelowSixtySeconds_ClampsToMinimumFloor()
    {

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(10));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.Zero);
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(-30));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(59));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that configuring valid floor values at or above 60 seconds updates the effective floor.
    /// </summary>
    [Fact]
    public void SetEffectiveFloor_WhenValueExceedsSixtySeconds_UpdatesEffectiveRetryFloor()
    {

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(120));

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(300));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(300));
    }

    /// <summary>
    /// Verifies that ResetEffectiveFloor restores the 60-second default floor.
    /// </summary>
    [Fact]
    public void ResetEffectiveFloor_RestoresMinimumSixtySecondFloor()
    {

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(180));
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(180));

        RateLimitPolicy.ResetEffectiveFloor();
        RateLimitPolicy.EffectiveRetryFloor.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Verifies that a raised effective floor elevates jittered backoff calculations.
    /// </summary>
    [Fact]
    public void CalculateDeadline_WhenFloorRaised_EnforcesRaisedFloorOnJitteredBackoff()
    {

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadlineFullJitter = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            1.0
        );

        var deadlineMinJitter = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var fourthFailure = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(120));

        var deadline = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(10));

        var deadline = RateLimitPolicy.CalculateDeadline(
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

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(60));

        var recordedDeadline = RateLimitPolicy.CalculateDeadline(
            BASE_TIME,
            null,
            1,
            1.0
        );

        recordedDeadline.Should().Be(BASE_TIME.AddSeconds(60));

        RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(300));

        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(59), recordedDeadline).Should().BeFalse();
        RateLimitPolicy.CanDispatch(BASE_TIME.AddSeconds(60), recordedDeadline).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that concurrent reads and updates of effective floor execute safely without errors or torn values.
    /// </summary>
    [Fact]
    public void ConcurrentAccess_ReadsAndUpdatesAreThreadSafe()
    {

        var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };

        Parallel.For(
            0,
            1000,
            parallelOptions,
            static i =>
            {

                var floorSeconds = (i % 3 + 1) * 60;

                RateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(floorSeconds));

                var currentFloor = RateLimitPolicy.EffectiveRetryFloor;

                currentFloor.Should().BeGreaterThanOrEqualTo(RateLimitPolicy.MINIMUM_RETRY_FLOOR);

                var deadline = RateLimitPolicy.CalculateDeadline(
                    BASE_TIME,
                    0,
                    1,
                    1.0
                );

                deadline.Should().BeOnOrAfter(BASE_TIME.Add(RateLimitPolicy.MINIMUM_RETRY_FLOOR));
            }
        );
    }
}
