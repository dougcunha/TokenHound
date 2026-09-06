using AwesomeAssertions;
using NSubstitute;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>Verifies coordinator polling cadence, provider dispatching, rate limits, and snapshot events in UsageStore.</summary>
public sealed class UsageStoreTests
{
    /// <summary>Verifies that RegisterProvider and RefreshNowAsync caches snapshot and fires SnapshotUpdated event.</summary>
    [Fact]
    public async Task RegisterProvider_And_RefreshNowAsync_CachesSnapshotAndFiresEvent()
    {

        using var store = new UsageStore();
        var expected = CreateSnapshot("claude");
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(expected));

        Snapshot? firedSnapshot = null;
        store.SnapshotUpdated += (_, s) => firedSnapshot = s;

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        store.CurrentSnapshots.Should().ContainKey("claude");
        store.CurrentSnapshots["claude"].Should().Be(expected);
        firedSnapshot.Should().Be(expected);
    }

    /// <summary>Verifies that RefreshNowAsync skips polling when a provider has an active future rate limit deadline.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenRateLimitDeadlineInFuture_SkipsProvider()
    {

        using var store = new UsageStore();
        var future = DateTimeOffset.UtcNow.AddMinutes(5);
        var limited = CreateSnapshot("claude", ProviderStatus.RateLimited, future);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(limited));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that RefreshNowAsync polls provider when the rate limit deadline has passed.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenRateLimitDeadlineInPast_PollsProvider()
    {

        using var store = new UsageStore();
        var past = DateTimeOffset.UtcNow.AddMinutes(-1);
        var limited = CreateSnapshot("claude", ProviderStatus.RateLimited, past);
        var fresh = CreateSnapshot("claude", ProviderStatus.Ok);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult(limited),
            ValueTask.FromResult(fresh)
        );

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
        store.CurrentSnapshots["claude"].Status.Should().Be(ProviderStatus.Ok);
    }

    /// <summary>Verifies that TickAsync triggers active refresh when a registered monitor reports busy.</summary>
    [Fact]
    public async Task TickAsync_WhenMonitorIsBusy_TriggersActiveRefresh()
    {

        using var store = new UsageStore(idleInterval: TimeSpan.FromMinutes(5));
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(CreateSnapshot("claude")));

        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("claude");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AgentSession?>(new AgentSession
        {
            Pid = 1000,
            StartTimeUtc = DateTimeOffset.UtcNow,
            State = AgentSessionState.Busy,
            LastActivityUtc = DateTimeOffset.UtcNow
        }));

        store.RegisterProvider(provider);
        store.RegisterActivityMonitor(monitor);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.TickAsync(TestContext.Current.CancellationToken);

        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that TickAsync does not refresh when monitor is idle and idle interval has not elapsed.</summary>
    [Fact]
    public async Task TickAsync_WhenMonitorIsIdleAndIntervalNotElapsed_DoesNotRefresh()
    {

        using var store = new UsageStore(idleInterval: TimeSpan.FromMinutes(5));
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(CreateSnapshot("claude")));

        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("claude");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult<AgentSession?>(new AgentSession
        {
            Pid = 1000,
            StartTimeUtc = DateTimeOffset.UtcNow,
            State = AgentSessionState.Idle,
            LastActivityUtc = DateTimeOffset.UtcNow
        }));

        store.RegisterProvider(provider);
        store.RegisterActivityMonitor(monitor);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.TickAsync(TestContext.Current.CancellationToken);

        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that Dispose stops the periodic polling timer cleanly.</summary>
    [Fact]
    public void Start_And_Dispose_StopsTimerCleanly()
    {

        var store = new UsageStore();

        store.Start(TimeSpan.FromMilliseconds(50));
        store.IsRunning.Should().BeTrue();

        store.Dispose();
        store.IsRunning.Should().BeFalse();
    }

    /// <summary>Verifies that Start with short poll interval executes background ticks and updates snapshots.</summary>
    [Fact]
    public async Task Start_WithShortPollInterval_ExecutesBackgroundTicks()
    {

        using var store = new UsageStore();
        var tcs = new TaskCompletionSource<Snapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = CreateSnapshot("claude");

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(expected));

        store.SnapshotUpdated += (_, s) => tcs.TrySetResult(s);
        store.RegisterProvider(provider);

        store.Start(TimeSpan.FromMilliseconds(20));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        cts.Token.Register(() => tcs.TrySetCanceled());

        var received = await tcs.Task;
        received.Should().Be(expected);
    }

    /// <summary>Verifies that RefreshNowAsync creates a stale snapshot when a provider throws an exception.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderThrows_StoresStaleSnapshot()
    {

        using var store = new UsageStore();
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("faulty");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns<ValueTask<Snapshot>>(_ => throw new InvalidOperationException("API unavailable"));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        store.CurrentSnapshots.Should().ContainKey("faulty");
        var snapshot = store.CurrentSnapshots["faulty"];
        snapshot.Status.Should().Be(ProviderStatus.Stale);
        snapshot.ErrorDescription.Should().Be("API unavailable");
    }

    /// <summary>Verifies that RefreshNowAsync throws OperationCanceledException when cancellation is requested.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
    {

        using var store = new UsageStore();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await store.RefreshNowAsync(cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Snapshot CreateSnapshot(
        string providerId,
        ProviderStatus status = ProviderStatus.Ok,
        DateTimeOffset? resetTime = null)
    {

        UsageBlock? block = resetTime.HasValue
            ? new UsageBlock
            {
                Reason = "Rate limited",
                IsBlocked = true,
                ResetTimeUtc = resetTime,
                RetryAfterSeconds = 60
            }
            : null;

        return new Snapshot
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = block,
            ErrorDescription = null
        };
    }
}
