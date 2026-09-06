using AwesomeAssertions;
using System;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies active and idle polling schedule evaluation in <see cref="RefreshSchedulePolicy"/>.
/// </summary>
public sealed class RefreshSchedulePolicyTests
{
    private static readonly TimeSpan CUSTOM_IDLE_INTERVAL = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Verifies that ShouldRefresh returns true whenever any agent is busy, regardless of elapsed time.
    /// </summary>
    [Fact]
    public void ShouldRefresh_WhenAgentIsBusy_ReturnsTrueImmediately()
    {
        var zeroElapsedResult = RefreshSchedulePolicy.ShouldRefresh(
            true,
            TimeSpan.Zero,
            CUSTOM_IDLE_INTERVAL
        );

        var shortElapsedResult = RefreshSchedulePolicy.ShouldRefresh(
            true,
            TimeSpan.FromSeconds(5),
            CUSTOM_IDLE_INTERVAL
        );

        zeroElapsedResult.Should().BeTrue();
        shortElapsedResult.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ShouldRefresh returns false when agents are idle and elapsed duration is below the idle interval.
    /// </summary>
    [Fact]
    public void ShouldRefresh_WhenIdleAndIntervalNotElapsed_ReturnsFalse()
    {
        var result = RefreshSchedulePolicy.ShouldRefresh(
            false,
            TimeSpan.FromSeconds(119),
            CUSTOM_IDLE_INTERVAL
        );

        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShouldRefresh returns true when agents are idle and elapsed duration meets or exceeds the idle interval.
    /// </summary>
    [Fact]
    public void ShouldRefresh_WhenIdleAndIntervalElapsed_ReturnsTrue()
    {
        var exactResult = RefreshSchedulePolicy.ShouldRefresh(
            false,
            TimeSpan.FromSeconds(120),
            CUSTOM_IDLE_INTERVAL
        );

        var pastResult = RefreshSchedulePolicy.ShouldRefresh(
            false,
            TimeSpan.FromSeconds(121),
            CUSTOM_IDLE_INTERVAL
        );

        exactResult.Should().BeTrue();
        pastResult.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the default overload of ShouldRefresh using the 300-second idle interval.
    /// </summary>
    [Fact]
    public void ShouldRefresh_WithDefaultIdleInterval_BehavesCorrectly()
    {
        var busyShort = RefreshSchedulePolicy.ShouldRefresh(true, TimeSpan.FromSeconds(10));
        var idleBelow = RefreshSchedulePolicy.ShouldRefresh(false, TimeSpan.FromSeconds(299));
        var idleExact = RefreshSchedulePolicy.ShouldRefresh(false, TimeSpan.FromSeconds(300));
        var idleAbove = RefreshSchedulePolicy.ShouldRefresh(false, TimeSpan.FromSeconds(301));

        busyShort.Should().BeTrue();
        idleBelow.Should().BeFalse();
        idleExact.Should().BeTrue();
        idleAbove.Should().BeTrue();
    }

    /// <summary>
    /// Verifies default interval and threshold constants on <see cref="RefreshSchedulePolicy"/>.
    /// </summary>
    [Fact]
    public void RefreshSchedulePolicy_DefaultConstants_MatchSpecifications()
    {
        RefreshSchedulePolicy.DefaultActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
        RefreshSchedulePolicy.DefaultIdleInterval.Should().Be(TimeSpan.FromSeconds(300));
        RefreshSchedulePolicy.DefaultStaleThreshold.Should().Be(TimeSpan.FromSeconds(900));

        RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL.Should().Be(TimeSpan.FromSeconds(60));
        RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL.Should().Be(TimeSpan.FromSeconds(300));
        RefreshSchedulePolicy.DEFAULT_STALE_THRESHOLD.Should().Be(TimeSpan.FromSeconds(900));
    }
}
