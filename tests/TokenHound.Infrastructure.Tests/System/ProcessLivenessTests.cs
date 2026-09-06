using AwesomeAssertions;
using System;
using TokenHound.Infrastructure.System;
using Xunit;

namespace TokenHound.Infrastructure.Tests.System;

/// <summary>
/// Verifies process liveness validation and start-time matching to guard against PID recycling.
/// </summary>
public sealed class ProcessLivenessTests
{
    /// <summary>
    /// Verifies that checking the current process ID with matching start time returns <see langword="true"/>.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenCurrentProcessWithMatchingStartTime_ReturnsTrue()
    {

        var currentPid = Environment.ProcessId;
        var startTimeUtc = ProcessLiveness.GetProcessStartTimeUtc(currentPid);

        startTimeUtc.Should().NotBeNull();

        var isAlive = ProcessLiveness.IsProcessAlive(currentPid, startTimeUtc);

        isAlive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that checking the current process ID with a mismatched start time returns <see langword="false"/>.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenCurrentProcessWithMismatchedStartTime_ReturnsFalse()
    {

        var currentPid = Environment.ProcessId;
        var mismatchedStartTimeUtc = DateTimeOffset.UtcNow.AddHours(-2);

        var isAlive = ProcessLiveness.IsProcessAlive(currentPid, mismatchedStartTimeUtc);

        isAlive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that checking a non-existent PID returns <see langword="false"/> without throwing.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenPidDoesNotExist_ReturnsFalseWithoutThrowing()
    {

        const int nonExistentPid = 99999999;

        var isAliveWithoutTime = ProcessLiveness.IsProcessAlive(nonExistentPid);
        var isAliveWithTime = ProcessLiveness.IsProcessAlive(nonExistentPid, DateTimeOffset.UtcNow);

        isAliveWithoutTime.Should().BeFalse();
        isAliveWithTime.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that negative or zero PIDs return <see langword="false"/> immediately.
    /// </summary>
    /// <param name="pid">The invalid process identifier to test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void IsProcessAlive_WhenPidIsNegativeOrZero_ReturnsFalse(int pid)
    {

        var isAliveWithoutTime = ProcessLiveness.IsProcessAlive(pid);
        var isAliveWithTime = ProcessLiveness.IsProcessAlive(pid, DateTimeOffset.UtcNow);

        isAliveWithoutTime.Should().BeFalse();
        isAliveWithTime.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that checking liveness with null expected start time checks only process existence.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenExpectedStartTimeIsNull_ChecksProcessExistenceOnly()
    {

        var currentPid = Environment.ProcessId;

        var isAlive = ProcessLiveness.IsProcessAlive(currentPid, expectedStartTimeUtc: null);

        isAlive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that checking liveness with a start time within custom tolerance returns <see langword="true"/>.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenTimestampWithinCustomTolerance_ReturnsTrue()
    {

        var currentPid = Environment.ProcessId;
        var actualStartUtc = ProcessLiveness.GetProcessStartTimeUtc(currentPid);

        actualStartUtc.Should().NotBeNull();

        var shiftedStartTime = actualStartUtc!.Value.AddMilliseconds(400);
        var isAlive = ProcessLiveness.IsProcessAlive(
            currentPid,
            shiftedStartTime,
            TimeSpan.FromSeconds(1.0)
        );

        isAlive.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that checking liveness with a start time exceeding custom tolerance returns <see langword="false"/>.
    /// </summary>
    [Fact]
    public void IsProcessAlive_WhenTimestampExceedsCustomTolerance_ReturnsFalse()
    {

        var currentPid = Environment.ProcessId;
        var actualStartUtc = ProcessLiveness.GetProcessStartTimeUtc(currentPid);

        actualStartUtc.Should().NotBeNull();

        var shiftedStartTime = actualStartUtc!.Value.AddSeconds(5);
        var isAlive = ProcessLiveness.IsProcessAlive(
            currentPid,
            shiftedStartTime,
            TimeSpan.FromSeconds(1.0)
        );

        isAlive.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GetProcessStartTimeUtc returns a valid UTC timestamp for the current process.
    /// </summary>
    [Fact]
    public void GetProcessStartTimeUtc_WhenCurrentProcess_ReturnsValidUtcTimestamp()
    {

        var currentPid = Environment.ProcessId;

        var startTimeUtc = ProcessLiveness.GetProcessStartTimeUtc(currentPid);

        startTimeUtc.Should().NotBeNull();
        startTimeUtc!.Value.Offset.Should().Be(TimeSpan.Zero);
        startTimeUtc.Value.Should().BeOnOrBefore(DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Verifies that GetProcessStartTimeUtc returns <see langword="null"/> for invalid or non-existent PIDs.
    /// </summary>
    /// <param name="pid">The invalid or non-existent process identifier.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99999999)]
    public void GetProcessStartTimeUtc_WhenPidIsInvalidOrNonExistent_ReturnsNull(int pid)
    {

        var result = ProcessLiveness.GetProcessStartTimeUtc(pid);

        result.Should().BeNull();
    }
}
