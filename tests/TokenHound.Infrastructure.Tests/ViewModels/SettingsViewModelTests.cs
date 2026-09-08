using AwesomeAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Engine;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies that <see cref="SettingsViewModel"/> lists every registered provider in a stable order (TC-14),
/// applies a toggle to the engine gate, the badge, and persistence (TC-13), keeps the disk write off the
/// caller, refreshes badges from live snapshots, and unsubscribes on disposal.
/// </summary>
public sealed class SettingsViewModelTests
{
    private const int FIVE_PROVIDERS = 5;
    private const int SIX_PROVIDERS = 6;

    /// <summary>
    /// Verifies that rows are built from the live registrations, sorted by catalog display name, and carry
    /// the catalog display name and glyph key of each provider (TC-14).
    /// </summary>
    [Fact]
    public void Constructor_WhenStoreHasFiveProviders_BuildsNameSortedRowsFromCatalog()
    {

        using var store = CreateStore("cursor", "claude", "copilot", "antigravity", "codex");
        using var viewModel = CreateViewModel(store, out _);

        viewModel.Providers.Should().HaveCount(FIVE_PROVIDERS);

        viewModel.Providers.Select(static row => row.ProviderName).Should().ContainInOrder(
            "Antigravity",
            "Claude Code",
            "Codex",
            "Copilot",
            "Cursor"
        );

        viewModel.Providers.Select(static row => row.GlyphKey).Should().ContainInOrder(
            "Glyph.Antigravity",
            "Glyph.Claude",
            "Glyph.Codex",
            "Glyph.Copilot",
            "Glyph.Cursor"
        );
    }

    /// <summary>
    /// Verifies that a sixth registered provider produces a sixth row without any code change (TC-14, R-1).
    /// </summary>
    [Fact]
    public void Constructor_WhenSixthProviderIsRegistered_ListsItWithoutCodeChange()
    {

        using var store = CreateStore("cursor", "claude", "copilot", "antigravity", "codex", "grok");
        using var viewModel = CreateViewModel(store, out _);

        viewModel.Providers.Should().HaveCount(SIX_PROVIDERS);
        viewModel.Providers[^1].ProviderId.Should().Be("grok");
        viewModel.Providers[^1].ProviderName.Should().Be("Grok");
    }

    /// <summary>
    /// Verifies that every row starts monitored and in the transient checking badge when no preference is
    /// stored and no snapshot has arrived yet (FR-02, A-04).
    /// </summary>
    [Fact]
    public void Constructor_WhenNoPreferenceIsStored_StartsEveryRowMonitored()
    {

        using var store = CreateStore("claude", "codex");
        using var viewModel = CreateViewModel(store, out _);

        viewModel.Providers.Should().OnlyContain(row => row.IsMonitored);
        viewModel.Providers.Should().OnlyContain(row => row.BadgeState == ProviderBadgeState.Checking);
    }

    /// <summary>
    /// Verifies that disabling a row flips the engine gate, moves the badge to disabled, and persists the
    /// full enablement map exactly once (TC-13).
    /// </summary>
    [Fact]
    public void IsMonitored_WhenSetToFalse_GatesEngineMovesBadgeAndPersistsOnce()
    {

        using var store = CreateStore("claude", "codex");
        using var viewModel = CreateViewModel(store, out var persisted);

        var row = viewModel.Providers.Single(static candidate => candidate.ProviderId == "claude");
        row.IsMonitored = false;

        store.IsProviderEnabled("claude").Should().BeFalse();
        store.IsProviderEnabled("codex").Should().BeTrue();
        row.BadgeState.Should().Be(ProviderBadgeState.Disabled);
        row.BadgeLabel.Should().Be("Disabled");

        persisted.Should().ContainSingle();
        persisted[0].IsEnabled("claude").Should().BeFalse();
        persisted[0].IsEnabled("codex").Should().BeTrue();
        persisted[0].EnabledStates.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that re-enabling a row moves the badge off disabled and dispatches an immediate refresh for
    /// that provider only (FR-04, US-04).
    /// </summary>
    [Fact]
    public async Task IsMonitored_WhenSetBackToTrue_ClearsDisabledBadgeAndRefreshesThatProvider()
    {

        var fetched = new TaskCompletionSource();
        var claude = CreateProvider("claude", () => fetched.TrySetResult());
        var codex = CreateProvider("codex");

        using var store = new UsageStore();
        store.RegisterProvider(claude);
        store.RegisterProvider(codex);

        using var viewModel = CreateViewModel(store, out var persisted);

        var row = viewModel.Providers.Single(static candidate => candidate.ProviderId == "claude");
        row.IsMonitored = false;
        row.IsMonitored = true;

        row.BadgeState.Should().NotBe(ProviderBadgeState.Disabled);
        store.IsProviderEnabled("claude").Should().BeTrue();

        await fetched.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        _ = codex.DidNotReceive().GetSnapshotAsync(Arg.Any<CancellationToken>());
        persisted.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that a persistence operation that never completes does not block the toggle setter, keeping
    /// the visual response independent of the disk write (NFR-04).
    /// </summary>
    [Fact]
    public void IsMonitored_WhenPersistenceNeverCompletes_DoesNotBlockTheSetter()
    {

        var pending = new TaskCompletionSource<bool>();

        using var store = CreateStore("claude");
        using var viewModel = new SettingsViewModel(store, (_, _) => pending.Task);

        var row = viewModel.Providers.Single();
        row.IsMonitored = false;

        store.IsProviderEnabled("claude").Should().BeFalse();
        row.BadgeState.Should().Be(ProviderBadgeState.Disabled);
        pending.Task.IsCompleted.Should().BeFalse();

        pending.SetResult(true);
    }

    /// <summary>
    /// Verifies that a snapshot arriving for a listed provider refreshes that row's badge live (FR-05).
    /// </summary>
    [Fact]
    public async Task OnSnapshotUpdated_WhenSnapshotArrives_RefreshesRowBadge()
    {

        using var store = new UsageStore();
        store.RegisterProvider(CreateProvider("claude", status: ProviderStatus.NeedsAuth));

        using var viewModel = CreateViewModel(store, out _);

        var row = viewModel.Providers.Single();
        row.BadgeState.Should().Be(ProviderBadgeState.Checking);

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        row.BadgeState.Should().Be(ProviderBadgeState.NeedsAuth);
        row.BadgeLabel.Should().Be("Needs Auth");
    }

    /// <summary>
    /// Verifies that disposal unsubscribes from the snapshot event, so later snapshots no longer mutate rows.
    /// </summary>
    [Fact]
    public async Task Dispose_WhenSnapshotArrivesAfterwards_LeavesRowBadgeUntouched()
    {

        using var store = new UsageStore();
        store.RegisterProvider(CreateProvider("claude"));

        var viewModel = CreateViewModel(store, out _);
        var row = viewModel.Providers.Single();

        viewModel.Dispose();

        await store.RefreshNowAsync(TestContext.Current.CancellationToken);

        row.BadgeState.Should().Be(ProviderBadgeState.Checking);
    }

    private static SettingsViewModel CreateViewModel(UsageStore store, out List<ProviderSettings> persisted)
    {

        var captured = new List<ProviderSettings>();
        persisted = captured;

        return new SettingsViewModel(
            store,
            (settings, _) =>
            {

                captured.Add(settings);

                return Task.FromResult(true);
            }
        );
    }

    private static UsageStore CreateStore(params string[] providerIds)
    {

        var store = new UsageStore();

        foreach (var providerId in providerIds)
            store.RegisterProvider(CreateProvider(providerId));

        return store;
    }

    private static IUsageProvider CreateProvider(
        string providerId,
        Action? onFetch = null,
        ProviderStatus status = ProviderStatus.Ok)
    {

        var provider = Substitute.For<IUsageProvider>();
        provider.ProviderId.Returns(providerId);

        provider.GetSnapshotAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {

            onFetch?.Invoke();

            return ValueTask.FromResult(CreateSnapshot(providerId, status));
        });

        return provider;
    }

    private static Snapshot CreateSnapshot(string providerId, ProviderStatus status)
        => new()
        {
            ProviderId = providerId,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = null
        };
}
