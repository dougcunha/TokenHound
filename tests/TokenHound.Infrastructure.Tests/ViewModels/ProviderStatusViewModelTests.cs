using AwesomeAssertions;
using NSubstitute;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies enablement filtering, live updates, countdown refresh, and disposal of <see cref="ProviderStatusViewModel"/>.
/// </summary>
public sealed class ProviderStatusViewModelTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 26, 22, 23, 0, TimeSpan.Zero);

    /// <summary>Verifies that only enabled providers appear, with a pending row for one without a snapshot.</summary>
    [Fact]
    public async Task Constructor_WhenProvidersMixed_ShowsEnabledAndPendingOnly()
    {

        using var store = new UsageStore();
        RegisterProvider(store, "claude", 0.3);
        RegisterProvider(store, "codex", 0.5);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        store.RegisterProvider(CreateProvider("cursor"));
        store.SetProviderEnabled("codex", false);

        using var viewModel = new ProviderStatusViewModel(store, formatter: CreateFormatter(new ManualTimeProvider(FIXED_NOW)));

        viewModel.Groups.Select(static g => g.FamilyName).Should().Equal("Claude", "Cursor");
        viewModel.Groups[1].Accounts.Single().IsPending.Should().BeTrue();
        viewModel.IsEmpty.Should().BeFalse();
    }

    /// <summary>Verifies the empty state when no provider is registered or enabled.</summary>
    [Fact]
    public void Constructor_WhenNoProviders_IsEmpty()
    {

        using var store = new UsageStore();

        using var viewModel = new ProviderStatusViewModel(store, formatter: CreateFormatter(new ManualTimeProvider(FIXED_NOW)));

        viewModel.Groups.Should().BeEmpty();
        viewModel.IsEmpty.Should().BeTrue();
    }

    /// <summary>Verifies that new snapshots and enablement changes rebuild the groups through the dispatcher.</summary>
    [Fact]
    public async Task StoreEvents_WhileOpen_RebuildThroughDispatcher()
    {

        using var store = new UsageStore();
        var dispatched = 0;
        using var viewModel = new ProviderStatusViewModel(
            store,
            action => { dispatched++; action(); },
            CreateFormatter(new ManualTimeProvider(FIXED_NOW))
        );

        RegisterProvider(store, "codex", 0.5);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        viewModel.Groups.Select(static g => g.FamilyName).Should().Equal("Codex");
        dispatched.Should().BeGreaterThan(0);

        store.SetProviderEnabled("codex", false);

        viewModel.Groups.Should().BeEmpty();
        viewModel.IsEmpty.Should().BeTrue();
    }

    /// <summary>Verifies that the minute tick re-projects countdowns and that disposal stops every update.</summary>
    [Fact]
    public async Task TimerTick_RefreshesCountdownsUntilDisposed()
    {

        using var store = new UsageStore();
        var time = new ManualTimeProvider(FIXED_NOW);
        var dispatched = 0;
        RegisterProvider(store, "claude", 0.3);
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);
        var viewModel = new ProviderStatusViewModel(store, action => { dispatched++; action(); }, CreateFormatter(time));

        ResetText(viewModel).Should().StartWith("in 2 hours");
        time.Advance(TimeSpan.FromMinutes(61));
        ResetText(viewModel).Should().StartWith("in 59 minutes");

        viewModel.Dispose();
        viewModel.Dispose();
        var dispatchedAtDispose = dispatched;
        time.Advance(TimeSpan.FromMinutes(1));
        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        dispatched.Should().Be(dispatchedAtDispose);
        time.ActiveTimerCount.Should().Be(0);
    }

    private static string ResetText(ProviderStatusViewModel viewModel)
        => viewModel.Groups.Single().Accounts.Single().Columns.Single().ResetText;

    private static ProviderStatusFormatter CreateFormatter(ManualTimeProvider time)
        => new(time, CultureInfo.InvariantCulture);

    private static IUsageProvider CreateProvider(string providerId)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns(providerId);

        return provider;
    }

    private static void RegisterProvider(UsageStore store, string providerId, double usedFraction)
    {

        var snapshot = new Snapshot
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow { Name = "window", UsedFraction = usedFraction, ResetTimeUtc = FIXED_NOW.AddHours(2) }
            ]
        };
        var provider = CreateProvider(providerId);
        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(snapshot));

        store.RegisterProvider(provider);
    }
}
