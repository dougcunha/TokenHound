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

    /// <summary>Verifies that a provider-local timeout does not stop subsequent eligible providers.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderTimesOutLocally_ContinuesToOtherProviders()
    {

        using var store = new UsageStore();
        var timedOutProvider = Substitute.For<IUsageProvider>();
        timedOutProvider.ProviderId.Returns("slow");
        using var localCts = new CancellationTokenSource();
        localCts.Cancel();
        timedOutProvider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(_ => throw new OperationCanceledException(localCts.Token));

        var healthySnapshot = CreateSnapshot("healthy");
        var healthyProvider = Substitute.For<IUsageProvider>();
        healthyProvider.ProviderId.Returns("healthy");
        healthyProvider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(healthySnapshot));

        store.RegisterProvider(timedOutProvider);
        store.RegisterProvider(healthyProvider);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        store.CurrentSnapshots.Should().ContainKey("slow");
        store.CurrentSnapshots["slow"].Status.Should().Be(ProviderStatus.Stale);
        store.CurrentSnapshots.Should().ContainKey("healthy");
        store.CurrentSnapshots["healthy"].Status.Should().Be(ProviderStatus.Ok);
    }

    /// <summary>Verifies that caller cancellation stops the refresh cycle and aborts subsequent providers.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenCallerCancels_AbortsRemainingProviders()
    {

        using var store = new UsageStore();
        using var callerCts = new CancellationTokenSource();

        var firstProvider = Substitute.For<IUsageProvider>();
        firstProvider.ProviderId.Returns("first");
        firstProvider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(_ =>
            {
                callerCts.Cancel();
                throw new OperationCanceledException(callerCts.Token);
            });

        var secondProvider = Substitute.For<IUsageProvider>();
        secondProvider.ProviderId.Returns("second");
        secondProvider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(CreateSnapshot("second")));

        store.RegisterProvider(firstProvider);
        store.RegisterProvider(secondProvider);

        var act = async () => await store.RefreshNowAsync(callerCts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        await secondProvider.DidNotReceive().GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that engine-generated stale snapshots retain previous sample provenance and null fractions.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderThrows_RetainsPreviousSnapshotProvenance()
    {

        using var store = new UsageStore();
        var originalTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var block = new UsageBlock { Reason = "Blocked", IsBlocked = true, ResetTimeUtc = DateTimeOffset.UtcNow.AddMinutes(-5), RetryAfterSeconds = 300 };
        var window = new LimitWindow { Name = "Session", RemainingUnits = 10, TotalUnits = null, UsedFraction = null };
        var initial = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = originalTime,
            LimitWindows = [window],
            ActiveBlock = block,
            ErrorDescription = null
        };

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        var callCount = 0;
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns<ValueTask<Snapshot>>(_ =>
        {
            callCount++;

            if (callCount == 1)
                return ValueTask.FromResult(initial);

            throw new InvalidOperationException("Network down");
        });

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        var stale = store.CurrentSnapshots["claude"];
        stale.Status.Should().Be(ProviderStatus.Stale);
        stale.Fidelity.Should().Be(Fidelity.Derived);
        stale.FetchedAtUtc.Should().Be(originalTime);
        stale.ActiveBlock.Should().Be(block);
        stale.LimitWindows.Should().HaveCount(1);
        stale.LimitWindows[0].UsedFraction.Should().BeNull();
        stale.ErrorDescription.Should().Be("Network down");
    }

    /// <summary>Verifies that unchanged snapshots are republished via SnapshotUpdated event on manual refresh.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenUnchangedSnapshot_StillPublishesEvent()
    {

        using var store = new UsageStore();
        var snapshot = CreateSnapshot("claude");
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(snapshot));

        var eventCount = 0;
        store.SnapshotUpdated += (_, _) => eventCount++;
        store.RegisterProvider(provider);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        eventCount.Should().Be(2);
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
