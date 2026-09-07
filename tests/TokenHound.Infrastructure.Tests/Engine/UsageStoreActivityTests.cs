using AwesomeAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies the independent activity polling timer and activity event plumbing.
/// </summary>
public sealed class UsageStoreActivityTests
{
    /// <summary>
    /// Verifies that activity polling publishes busy and then idle states without quota refreshes.
    /// </summary>
    [Fact]
    public async Task ActivityTimer_PublishesBusyAndIdleStates()
    {
        using var store = new UsageStore(
            idleInterval: TimeSpan.FromHours(1),
            pollInterval: TimeSpan.FromHours(1),
            activityPollInterval: TimeSpan.FromMilliseconds(10)
        );
        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("copilot");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult<AgentSession?>(CreateSession(AgentSessionState.Busy))
        );
        var changes = new TaskCompletionSource<ProviderActivityChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        store.ActivityUpdated += (_, args) => changes.TrySetResult(args);
        store.RegisterActivityMonitor(monitor);

        store.Start(
            TimeSpan.FromHours(1),
            TimeSpan.FromMilliseconds(10)
        );
        var busy = await changes.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken
        );

        busy.ProviderId.Should().Be("copilot");
        busy.AgentSession!.State.Should().Be(AgentSessionState.Busy);
        store.IsActivityRunning.Should().BeTrue();

        var idle = new TaskCompletionSource<ProviderActivityChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        store.ActivityUpdated += (_, args) =>
        {

            if (args.AgentSession is null)
                idle.TrySetResult(args);
        };
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult<AgentSession?>(null)
        );

        var idleChange = await idle.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken
        );

        idleChange.ProviderId.Should().Be("copilot");
        idleChange.AgentSession.Should().BeNull();
        await store.StopAsync(TestContext.Current.CancellationToken);
        store.IsActivityRunning.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that monitor failure publishes an idle state rather than retaining a ghost busy state.
    /// </summary>
    [Fact]
    public async Task ActivityTimer_WhenMonitorFails_PublishesIdleState()
    {
        using var store = new UsageStore(activityPollInterval: TimeSpan.FromMilliseconds(10));
        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("copilot");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns<ValueTask<AgentSession?>>(_ =>
            throw new InvalidOperationException("monitor failed"));
        var change = new TaskCompletionSource<ProviderActivityChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        store.ActivityUpdated += (_, args) => change.TrySetResult(args);
        store.RegisterActivityMonitor(monitor);

        store.Start(
            TimeSpan.FromHours(1),
            TimeSpan.FromMilliseconds(10)
        );
        var result = await change.Task.WaitAsync(
            TimeSpan.FromSeconds(2),
            TestContext.Current.CancellationToken
        );

        result.AgentSession.Should().BeNull();
        await store.StopAsync(TestContext.Current.CancellationToken);
    }

    private static AgentSession CreateSession(AgentSessionState state)
        => new()
        {
            Pid = 42,
            StartTimeUtc = DateTimeOffset.UtcNow,
            State = state,
            LastActivityUtc = DateTimeOffset.UtcNow
        };
}
