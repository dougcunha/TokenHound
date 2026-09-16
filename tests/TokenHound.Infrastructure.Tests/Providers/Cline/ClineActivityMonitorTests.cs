using AwesomeAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Cline;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers.Cline;

/// <summary>
/// Verifies <see cref="ClineActivityMonitor"/> hub-first liveness and activity-driven session states.
/// </summary>
public sealed class ClineActivityMonitorTests
{
    private static readonly DateTimeOffset HUB_START = new(2026, 9, 16, 18, 26, 9, TimeSpan.Zero);
    private static readonly DateTimeOffset RECENT_ACTIVITY = new(2026, 9, 16, 19, 59, 30, TimeSpan.Zero);
    private static readonly DateTimeOffset NOW = new(2026, 9, 16, 20, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that ProviderId returns "cline".</summary>
    [Fact]
    public void ProviderId_ReturnsCline()
    {

        var monitor = new ClineActivityMonitor();

        monitor.ProviderId.Should().Be("cline");
    }

    /// <summary>Verifies that a live hub with recent database activity reports a busy session.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithLiveHubAndRecentActivity_ReturnsBusy()
    {

        var monitor = new ClineActivityMonitor(
            processLocator: static () => null,
            timeProvider: new FakeTimeProvider(NOW),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(RECENT_ACTIVITY),
            processLiveness: static (_, _) => true,
            hubReader: static _ => Task.FromResult<ClineHubSnapshot?>(new ClineHubSnapshot
            {
                Pid = 38116,
                StartedAtUtc = HUB_START
            })
        );

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.Pid.Should().Be(38116);
        session.State.Should().Be(AgentSessionState.Busy);
        session.LastActivityUtc.Should().Be(RECENT_ACTIVITY);
    }

    /// <summary>Verifies that a live hub with stale database activity reports an idle session.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithLiveHubAndStaleActivity_ReturnsIdle()
    {

        var monitor = new ClineActivityMonitor(
            processLocator: static () => null,
            timeProvider: new FakeTimeProvider(NOW),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(NOW.AddMinutes(-10)),
            processLiveness: static (_, _) => true,
            hubReader: static _ => Task.FromResult<ClineHubSnapshot?>(new ClineHubSnapshot
            {
                Pid = 38116,
                StartedAtUtc = HUB_START
            })
        );

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.State.Should().Be(AgentSessionState.Idle);
    }

    /// <summary>Verifies that a dead hub falls back to the process name lookup.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithDeadHub_FallsBackToProcess()
    {

        var startedAt = NOW.AddHours(-1);
        var monitor = new ClineActivityMonitor(
            processLocator: () => (4242, startedAt),
            timeProvider: new FakeTimeProvider(NOW),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(null),
            processLiveness: static (_, _) => true,
            hubReader: static _ => Task.FromResult<ClineHubSnapshot?>(null)
        );

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().NotBeNull();
        session!.Pid.Should().Be(4242);
        session.State.Should().Be(AgentSessionState.Idle);
    }

    /// <summary>Verifies that no hub and no process produce no session.</summary>
    [Fact]
    public async Task CheckLivenessAsync_WithNothingRunning_ReturnsNull()
    {

        var monitor = new ClineActivityMonitor(
            processLocator: static () => null,
            timeProvider: new FakeTimeProvider(NOW),
            activityReader: static _ => Task.FromResult<DateTimeOffset?>(NOW),
            processLiveness: static (_, _) => false,
            hubReader: static _ => Task.FromResult<ClineHubSnapshot?>(null)
        );

        var session = await monitor.CheckLivenessAsync(TestContext.Current.CancellationToken);

        session.Should().BeNull();
    }

    private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}