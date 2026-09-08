using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Tests.Engine;

/// <summary>
/// Verifies per-provider enablement gating of refresh dispatch, activity polling, and the cadence decision.
/// </summary>
public sealed class UsageStoreGatingTests
{
    /// <summary>Verifies that a manual refresh never dispatches a disabled provider.</summary>
    [Fact]
    public async Task RefreshNowAsync_WhenProviderIsDisabled_SkipsThatProviderOnly()
    {

        using var store = new UsageStore();
        var enabled = CreateProvider("claude");
        var disabled = CreateProvider("codex");
        store.RegisterProvider(enabled);
        store.RegisterProvider(disabled);

        store.SetProviderEnabled("codex", false);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        await enabled.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
        await disabled.DidNotReceive().GetSnapshotAsync(Arg.Any<CancellationToken>());
        store.IsProviderEnabled("codex").Should().BeFalse();
        store.IsProviderEnabled("claude").Should().BeTrue();
    }

    /// <summary>Verifies that the periodic tick path gates identically to the manual refresh path.</summary>
    [Fact]
    public async Task TickAsync_WhenProviderIsDisabled_SkipsThatProviderOnly()
    {

        using var store = new UsageStore();
        var enabled = CreateProvider("claude");
        var disabled = CreateProvider("codex");
        store.RegisterProvider(enabled);
        store.RegisterProvider(disabled);

        store.SetProviderEnabled("codex", false);
        await store.TickAsync(TestContext.Current.CancellationToken);

        await enabled.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
        await disabled.DidNotReceive().GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the cadence probe never inspects a disabled provider's activity monitor.</summary>
    [Fact]
    public async Task TickAsync_WhenProviderIsDisabled_NeverChecksItsActivityMonitor()
    {

        using var store = new UsageStore();
        var enabledMonitor = CreateMonitor("claude", AgentSessionState.Busy);
        var disabledMonitor = CreateMonitor("codex", AgentSessionState.Busy);
        store.RegisterProvider(CreateProvider("claude"));
        store.RegisterProvider(CreateProvider("codex"));
        store.RegisterActivityMonitor(enabledMonitor);
        store.RegisterActivityMonitor(disabledMonitor);

        store.SetProviderEnabled("codex", false);
        await store.TickAsync(TestContext.Current.CancellationToken);

        await enabledMonitor.Received().CheckLivenessAsync(Arg.Any<CancellationToken>());
        await disabledMonitor.DidNotReceive().CheckLivenessAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that the activity polling loop never inspects a disabled provider's monitor.</summary>
    [Fact]
    public async Task ActivityPolling_WhenProviderIsDisabled_NeverChecksItsMonitor()
    {

        using var store = CreateTimedStore();
        var disabledMonitor = CreateMonitor("codex", AgentSessionState.Busy);
        store.RegisterProvider(CreateProvider("claude"));
        store.RegisterProvider(CreateProvider("codex"));
        store.RegisterActivityMonitor(CreateMonitor("claude", AgentSessionState.Busy));
        store.RegisterActivityMonitor(disabledMonitor);
        store.SetProviderEnabled("codex", false);

        var busy = SubscribeToBusy(store);
        store.Start(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(10));
        var change = await busy.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        store.Stop();

        change.ProviderId.Should().Be("claude");
        await disabledMonitor.DidNotReceive().CheckLivenessAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that re-enabling raises the change event once and refreshes only that provider.</summary>
    [Fact]
    public async Task SetProviderEnabled_WhenReEnabled_RaisesEventOnceAndRefreshesThatProviderOnly()
    {

        using var store = new UsageStore();
        var reEnabled = CreateProvider("claude");
        var untouched = CreateProvider("codex");
        store.RegisterProvider(reEnabled);
        store.RegisterProvider(untouched);
        store.SetProviderEnabled("claude", false);

        var changes = new List<ProviderEnablementChangedEventArgs>();
        store.ProviderEnablementChanged += (_, args) => changes.Add(args);

        store.SetProviderEnabled("claude", true);
        await store.RefreshProviderNowAsync("claude", TestContext.Current.CancellationToken);

        changes.Should().ContainSingle();
        changes[0].ProviderId.Should().Be("claude");
        changes[0].IsEnabled.Should().BeTrue();
        await reEnabled.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
        await untouched.DidNotReceive().GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that repeating the same enablement value raises no further change events.</summary>
    [Fact]
    public void SetProviderEnabled_WhenValueIsUnchanged_RaisesNoEvent()
    {

        using var store = new UsageStore();
        store.RegisterProvider(CreateProvider("claude"));

        var changes = 0;
        store.ProviderEnablementChanged += (_, _) => changes++;

        store.SetProviderEnabled("claude", true);
        store.SetProviderEnabled("claude", false);
        store.SetProviderEnabled("claude", false);

        changes.Should().Be(1);
        store.IsProviderEnabled("claude").Should().BeFalse();
    }

    /// <summary>Verifies that gating an unregistered identifier is a no-op on every gating member.</summary>
    [Fact]
    public async Task SetProviderEnabled_WhenProviderIsUnregistered_IsNoOp()
    {

        using var store = new UsageStore();
        store.RegisterProvider(CreateProvider("claude"));

        var changes = 0;
        store.ProviderEnablementChanged += (_, _) => changes++;

        store.SetProviderEnabled("ghost", false);
        await store.RefreshProviderNowAsync("ghost", TestContext.Current.CancellationToken);

        changes.Should().Be(0);
        store.IsProviderEnabled("ghost").Should().BeTrue();
        store.RegisteredProviderIds.Should().ContainSingle(id => id == "claude");
    }

    /// <summary>Verifies that re-enabling does not dispatch while a persisted rate-limit deadline is unexpired.</summary>
    [Fact]
    public async Task RefreshProviderNowAsync_WhenRateLimitDeadlineIsUnexpired_DoesNotDispatch()
    {

        using var store = new UsageStore();
        var future = DateTimeOffset.UtcNow.AddMinutes(5);
        var provider = CreateProvider("codex", CreateSnapshot("codex", ProviderStatus.RateLimited, future));
        store.RegisterProvider(provider);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        store.SetProviderEnabled("codex", false);
        store.SetProviderEnabled("codex", true);
        await store.RefreshProviderNowAsync("codex", TestContext.Current.CancellationToken);

        await provider.Received(1).GetSnapshotAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Verifies that disabling a busy provider clears its activity state and releases the active cadence.</summary>
    [Fact]
    public async Task SetProviderEnabled_WhenBusyProviderIsDisabled_ClearsActivityStateAndReleasesActiveCadence()
    {

        using var store = CreateTimedStore();
        var unmonitored = CreateProvider("codex");
        store.RegisterProvider(CreateProvider("claude"));
        store.RegisterProvider(unmonitored);
        store.RegisterActivityMonitor(CreateMonitor("claude", AgentSessionState.Busy));

        var busy = SubscribeToBusy(store);
        store.Start(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(10));
        await busy.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        store.Stop();

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        await store.TickAsync(TestContext.Current.CancellationToken);

        ProviderActivityChangedEventArgs? cleared = null;
        store.ActivityUpdated += (_, args) => cleared = args;
        store.SetProviderEnabled("claude", false);

        await store.TickAsync(TestContext.Current.CancellationToken);

        cleared.Should().NotBeNull();
        cleared!.ProviderId.Should().Be("claude");
        cleared.AgentSession.Should().BeNull();
        await unmonitored.Received(2).GetSnapshotAsync(Arg.Any<CancellationToken>());
        store.CurrentSnapshots.Should().ContainKey("claude");
    }

    private static UsageStore CreateTimedStore()
        => new(
            idleInterval: TimeSpan.FromHours(1),
            pollInterval: TimeSpan.FromHours(1),
            activityPollInterval: TimeSpan.FromMilliseconds(10)
        );

    private static TaskCompletionSource<ProviderActivityChangedEventArgs> SubscribeToBusy(UsageStore store)
    {

        var busy = new TaskCompletionSource<ProviderActivityChangedEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        store.ActivityUpdated += (_, args) =>
        {

            if (args.AgentSession?.State == AgentSessionState.Busy)
                busy.TrySetResult(args);
        };

        return busy;
    }

    private static IUsageProvider CreateProvider(string providerId, Snapshot? snapshot = null)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns(providerId);
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult(snapshot ?? CreateSnapshot(providerId))
        );

        return provider;
    }

    private static IActivityMonitor CreateMonitor(string providerId, AgentSessionState state)
    {

        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns(providerId);
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult<AgentSession?>(new AgentSession
            {
                Pid = 4242,
                StartTimeUtc = DateTimeOffset.UtcNow,
                State = state,
                LastActivityUtc = DateTimeOffset.UtcNow
            })
        );

        return monitor;
    }

    private static Snapshot CreateSnapshot(
        string providerId,
        ProviderStatus status = ProviderStatus.Ok,
        DateTimeOffset? resetTimeUtc = null)
        => new()
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = resetTimeUtc.HasValue
                ? new UsageBlock
                {
                    Reason = "Rate limited",
                    IsBlocked = true,
                    ResetTimeUtc = resetTimeUtc,
                    RetryAfterSeconds = 60
                }
                : null,
            ErrorDescription = null
        };
}
