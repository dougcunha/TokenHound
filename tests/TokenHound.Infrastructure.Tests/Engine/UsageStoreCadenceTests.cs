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

/// <summary>
/// Verifies polling cadence updates, safety floor clamping, and non-interruption of in-flight refreshes in UsageStore.
/// </summary>
public sealed class UsageStoreCadenceTests
{
    /// <summary>Verifies that active intervals below 30 seconds are clamped to 30 seconds.</summary>
    [Fact]
    public void UpdateCadence_ClampsActiveInterval_WhenBelowThirtySeconds()
    {

        using var store = new UsageStore();

        store.UpdateCadence(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60));

        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(30));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(60));
    }

    /// <summary>Verifies that idle intervals below the safe active interval are clamped to the active interval.</summary>
    [Fact]
    public void UpdateCadence_ClampsIdleInterval_WhenBelowActiveInterval()
    {

        using var store = new UsageStore();

        store.UpdateCadence(TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(45));

        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(90));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(90));
    }

    /// <summary>Verifies that both intervals clamp to 30 seconds when both are below the safety floor.</summary>
    [Fact]
    public void UpdateCadence_ClampsBothIntervals_WhenBothAreBelowThirtySeconds()
    {

        using var store = new UsageStore();

        store.UpdateCadence(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));

        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(30));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(30));
    }

    /// <summary>Verifies that UpdateCadence on a disposed store throws ObjectDisposedException.</summary>
    [Fact]
    public void UpdateCadence_OnDisposedStore_ThrowsObjectDisposedException()
    {

        var store = new UsageStore();
        store.Dispose();

        var act = () => store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>Verifies that UpdateCadence on a terminally stopped store throws ObjectDisposedException.</summary>
    [Fact]
    public async Task UpdateCadence_OnStoppingStore_ThrowsObjectDisposedException()
    {

        var store = new UsageStore();
        await store.StopAsync(TestContext.Current.CancellationToken);

        var act = () => store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        act.Should().Throw<ObjectDisposedException>();
    }

    /// <summary>Verifies that UpdateCadence restarts the timer while keeping running state intact.</summary>
    [Fact]
    public void UpdateCadence_WhileRunning_UpdatesCadenceAndRestartsTimer()
    {

        using var store = new UsageStore();
        store.Start(TimeSpan.FromSeconds(180));
        store.IsRunning.Should().BeTrue();

        store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        store.IsRunning.Should().BeTrue();
        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>Verifies that UpdateCadence does not start the timer when the store is stopped.</summary>
    [Fact]
    public void UpdateCadence_WhenStopped_UpdatesCadenceWithoutStartingTimer()
    {

        using var store = new UsageStore();

        store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        store.IsRunning.Should().BeFalse();
        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>Verifies that UpdateCadence preserves snapshots and registered providers.</summary>
    [Fact]
    public async Task UpdateCadence_PreservesRegisteredProviders_AndSnapshots()
    {

        using var store = new UsageStore();
        var snapshot = CreateSnapshot("claude");
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        store.CurrentSnapshots.Should().ContainKey("claude");

        store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        store.CurrentSnapshots.Should().ContainKey("claude");
        store.RegisteredProviderIds.Should().Contain("claude");
    }

    /// <summary>Verifies that UpdateCadence dynamically modifies the idle threshold governing tick eligibility.</summary>
    [Fact]
    public async Task UpdateCadence_UpdatesIdleThreshold_AffectingTickEligibility()
    {

        var initialTime = new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(initialTime);
        using var store = new UsageStore(idleInterval: TimeSpan.FromSeconds(60), timeProvider: timeProvider);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(CreateSnapshot("claude")));

        store.RegisterProvider(provider);
        await store.TickAsync(TestContext.Current.CancellationToken);
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());

        timeProvider.CurrentTime = initialTime + TimeSpan.FromSeconds(50);
        await store.TickAsync(TestContext.Current.CancellationToken);
        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());

        store.UpdateCadence(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(45));
        await store.TickAsync(TestContext.Current.CancellationToken);
        await provider.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that UpdateCadence does not cancel an in-flight admitted provider refresh.</summary>
    [Fact]
    public async Task UpdateCadence_DoesNotCancel_InFlightAdmittedRefresh()
    {

        using var store = new UsageStore();
        var enteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                using var reg = ct.Register(() => canceledTcs.TrySetResult());
                enteredTcs.TrySetResult();

                await proceedTcs.Task.WaitAsync(ct).ConfigureAwait(false);

                return CreateSnapshot("claude");
            });

        store.RegisterProvider(provider);
        store.Start(TimeSpan.FromMilliseconds(20));

        await enteredTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        store.UpdateCadence(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(120));

        canceledTcs.Task.IsCompleted.Should().BeFalse();

        proceedTcs.TrySetResult();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!store.CurrentSnapshots.ContainsKey("claude") && !cts.IsCancellationRequested)
            await Task.Delay(10, cts.Token);

        store.CurrentSnapshots.Should().ContainKey("claude");
        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
        store.IdleInterval.Should().Be(TimeSpan.FromSeconds(120));
    }

    /// <summary>Verifies that Start replacement does not cancel an in-flight admitted provider refresh.</summary>
    [Fact]
    public async Task StartTimer_Replacement_DoesNotCancel_InFlightAdmittedRefresh()
    {

        using var store = new UsageStore();
        var enteredTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var proceedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceledTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Snapshot>>(async callInfo =>
            {
                var ct = callInfo.Arg<CancellationToken>();
                using var reg = ct.Register(() => canceledTcs.TrySetResult());
                enteredTcs.TrySetResult();

                await proceedTcs.Task.WaitAsync(ct).ConfigureAwait(false);

                return CreateSnapshot("claude");
            });

        store.RegisterProvider(provider);
        store.Start(TimeSpan.FromMilliseconds(20));

        await enteredTcs.Task.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        store.Start(TimeSpan.FromSeconds(60));

        canceledTcs.Task.IsCompleted.Should().BeFalse();

        proceedTcs.TrySetResult();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        while (!store.CurrentSnapshots.ContainsKey("claude") && !cts.IsCancellationRequested)
            await Task.Delay(10, cts.Token);

        store.CurrentSnapshots.Should().ContainKey("claude");
        store.ActiveInterval.Should().Be(TimeSpan.FromSeconds(60));
    }

    private static Snapshot CreateSnapshot(string providerId)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = null
        };

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset CurrentTime { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow()
            => CurrentTime;
    }
}
