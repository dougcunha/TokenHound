using AwesomeAssertions;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers.Mock;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies that <see cref="NotchViewModel"/> correctly binds snapshot changes,
/// enforces zero fake data invariants, activates mock fallback, and unsubscribes on disposal.
/// </summary>
public sealed class NotchViewModelTests
{
    /// <summary>
    /// Verifies that NotchViewModel receives snapshot updates from UsageStore and creates new ring view models.
    /// </summary>
    [Fact]
    public async Task OnSnapshotUpdated_WhenNewProviderEmitted_AddsRingViewModel()
    {

        using var store = new UsageStore();
        using var viewModel = new NotchViewModel(store);

        var snapshot = CreateSnapshot("claude", ProviderStatus.Ok, 0.42);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "claude");
        var ring = viewModel.Rings.First(r => r.ProviderId == "claude");
        ring.UsedFraction.Should().Be(0.42);
        ring.Status.Should().Be(ProviderStatus.Ok);
    }

    /// <summary>
    /// Verifies that NotchViewModel updates existing ring view models in-place when a provider emits new telemetry.
    /// </summary>
    [Fact]
    public async Task OnSnapshotUpdated_WhenExistingProviderEmitted_UpdatesExistingRing()
    {

        using var store = new UsageStore();
        using var viewModel = new NotchViewModel(store);

        var initial = CreateSnapshot("claude", ProviderStatus.Ok, 0.30);
        var updated = CreateSnapshot("claude", ProviderStatus.Ok, 0.75);

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult(initial),
            ValueTask.FromResult(updated)
        );

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "claude");
        viewModel.Rings[0].UsedFraction.Should().Be(0.30);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "claude");
        viewModel.Rings[0].UsedFraction.Should().Be(0.75);
    }

    /// <summary>
    /// Verifies that UsedFraction remains null when snapshot window capacity is unknown, enforcing Zero Fake Data.
    /// </summary>
    [Fact]
    public async Task OnSnapshotUpdated_WhenWindowTotalUnitsIsNull_EnforcesZeroFakeData()
    {

        using var store = new UsageStore();
        using var viewModel = new NotchViewModel(store);

        var snapshot = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Unmeasured",
                    RemainingUnits = 250,
                    TotalUnits = null,
                    UsedFraction = null
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        var ring = viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "claude").Subject;
        ring.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that NotchViewModel automatically loads MockUsageProvider fallback when rings are empty and mock is supplied.
    /// </summary>
    [Fact]
    public void Constructor_WhenMockProviderConfiguredAndRingsEmpty_ActivatesMockFallback()
    {

        using var store = new UsageStore();
        var mock = new MockUsageProvider("mock");

        using var viewModel = new NotchViewModel(store, mockProvider: mock);

        viewModel.IsFallbackActive.Should().BeTrue();
        viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "mock");
    }

    /// <summary>
    /// Verifies that LoadMockFallback registers mock provider into UsageStore and exposes mock ring.
    /// </summary>
    [Fact]
    public void LoadMockFallback_WhenExplicitlyCalled_RegistersMockAndExposesRing()
    {

        using var store = new UsageStore();
        using var viewModel = new NotchViewModel(store);

        viewModel.IsFallbackActive.Should().BeFalse();

        var mock = new MockUsageProvider("mock-explicit");
        viewModel.LoadMockFallback(mock);

        viewModel.IsFallbackActive.Should().BeTrue();
        viewModel.Rings.Should().ContainSingle(r => r.ProviderId == "mock-explicit");
    }

    /// <summary>
    /// Verifies that disposing NotchViewModel unsubscribes cleanly from UsageStore snapshot events.
    /// </summary>
    [Fact]
    public async Task Dispose_UnsubscribesFromUsageStore()
    {

        using var store = new UsageStore();
        var viewModel = new NotchViewModel(store);

        viewModel.Dispose();

        var snapshot = CreateSnapshot("claude", ProviderStatus.Ok, 0.50);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("claude");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        viewModel.Rings.Should().BeEmpty();
    }

    /// <summary>Verifies UsageStore activity changes reach the existing Copilot ring.</summary>
    [Fact]
    public async Task OnActivityUpdated_WhenCopilotRingExistsUpdatesBusyState()
    {
        using var store = new UsageStore(activityPollInterval: TimeSpan.FromMilliseconds(10));
        using var viewModel = new NotchViewModel(store);
        var snapshot = CreateSnapshot("copilot", ProviderStatus.Ok, 0.25);
        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns("copilot");
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));
        store.RegisterProvider(provider);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        var ring = viewModel.Rings.Single(r => r.ProviderId == "copilot");
        var busyChanged = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        ring.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProviderRingViewModel.IsBusy) && ring.IsBusy)
                busyChanged.TrySetResult(true);
        };

        var monitor = Substitute.For<IActivityMonitor>();
        monitor.ProviderId.Returns("copilot");
        monitor.CheckLivenessAsync(Arg.Any<CancellationToken>()).Returns(
            ValueTask.FromResult<AgentSession?>(new AgentSession
            {
                Pid = 42,
                StartTimeUtc = DateTimeOffset.UtcNow,
                State = AgentSessionState.Busy,
                LastActivityUtc = DateTimeOffset.UtcNow
            }));
        store.RegisterActivityMonitor(monitor);
        store.Start(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(10));

        await busyChanged.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(ring.IsBusy);
        await store.StopAsync(TestContext.Current.CancellationToken);
    }

    private static Snapshot CreateSnapshot(
        string providerId,
        ProviderStatus status,
        double fraction)
        => new()
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "five_hour",
                    UsedFraction = fraction,
                    RemainingUnits = (long)((1.0 - fraction) * 100),
                    TotalUnits = 100,
                    Period = TimeSpan.FromHours(5),
                    ResetTimeUtc = DateTimeOffset.UtcNow.AddHours(2)
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };
}
