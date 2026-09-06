using AwesomeAssertions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers.Mock;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Providers;

/// <summary>
/// Verifies behavior of <see cref="MockUsageProvider"/> across predefined scenarios,
/// custom snapshots, zero fake data invariants, and cancellation propagation.
/// </summary>
public sealed class MockUsageProviderTests
{
    /// <summary>
    /// Verifies that the parameterless constructor sets the default provider identifier and Normal scenario.
    /// </summary>
    [Fact]
    public async Task Constructor_WithDefaultProviderId_InitializesWithMockIdAndNormalScenario()
    {

        var provider = new MockUsageProvider();

        provider.ProviderId.Should().Be("mock");
        provider.CurrentScenario.Should().Be(MockScenario.Normal);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.ProviderId.Should().Be("mock");
        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.LimitWindows.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that a custom provider identifier is retained by the provider and its snapshots.
    /// </summary>
    [Fact]
    public async Task Constructor_WithCustomProviderId_InitializesWithProvidedId()
    {

        var provider = new MockUsageProvider("custom-mock");

        provider.ProviderId.Should().Be("custom-mock");

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.ProviderId.Should().Be("custom-mock");
    }

    /// <summary>
    /// Verifies that invalid provider identifiers throw an <see cref="ArgumentException"/>.
    /// </summary>
    /// <param name="invalidId">The invalid provider identifier to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespaceProviderId_ThrowsArgumentException(string? invalidId)
    {

        var act = () => new MockUsageProvider(invalidId!);

        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that the Normal scenario generates an Ok snapshot with session and weekly windows.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenScenarioNormal_ReturnsNormalSnapshot()
    {

        var provider = new MockUsageProvider();
        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);
        snapshot.ActiveBlock.Should().BeNull();
        snapshot.ErrorDescription.Should().BeNull();

        var sessionWindow = snapshot.LimitWindows.First(w => w.Name == "Session");
        sessionWindow.UsedFraction.Should().Be(0.20);
        sessionWindow.RemainingUnits.Should().Be(80);
        sessionWindow.TotalUnits.Should().Be(100);

        var weeklyWindow = snapshot.LimitWindows.First(w => w.Name == "Weekly");
        weeklyWindow.UsedFraction.Should().Be(0.15);
        weeklyWindow.RemainingUnits.Should().Be(85);
        weeklyWindow.TotalUnits.Should().Be(100);
    }

    /// <summary>
    /// Verifies that switching to the Warning scenario updates the weekly limit window to 85%.
    /// </summary>
    [Fact]
    public async Task SetScenario_WhenWarning_UpdatesSnapshotToWarningState()
    {

        var provider = new MockUsageProvider();

        provider.SetScenario(MockScenario.Warning);
        provider.CurrentScenario.Should().Be(MockScenario.Warning);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.Fidelity.Should().Be(Fidelity.Official);

        var weeklyWindow = snapshot.LimitWindows.First(w => w.Name == "Weekly");
        weeklyWindow.UsedFraction.Should().Be(0.85);
        weeklyWindow.RemainingUnits.Should().Be(15);
    }

    /// <summary>
    /// Verifies that switching to RateLimited produces an active 429 block with retry seconds.
    /// </summary>
    [Fact]
    public async Task SetScenario_WhenRateLimited_UpdatesSnapshotToRateLimitedState()
    {

        var provider = new MockUsageProvider();

        provider.SetScenario(MockScenario.RateLimited);
        provider.CurrentScenario.Should().Be(MockScenario.RateLimited);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.RateLimited);
        snapshot.ActiveBlock.Should().NotBeNull();
        snapshot.ActiveBlock!.Reason.Should().Be("HTTP 429");
        snapshot.ActiveBlock.IsBlocked.Should().BeTrue();
        snapshot.ActiveBlock.RetryAfterSeconds.Should().Be(600);
        snapshot.ActiveBlock.ResetTimeUtc.Should().BeAfter(DateTimeOffset.UtcNow);
        snapshot.LimitWindows.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that switching to NeedsAuth produces an unauthenticated status and error message.
    /// </summary>
    [Fact]
    public async Task SetScenario_WhenNeedsAuth_UpdatesSnapshotToNeedsAuthState()
    {

        var provider = new MockUsageProvider();

        provider.SetScenario(MockScenario.NeedsAuth);
        provider.CurrentScenario.Should().Be(MockScenario.NeedsAuth);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.NeedsAuth);
        snapshot.ErrorDescription.Should().Be("Execute login in terminal");
        snapshot.ActiveBlock.Should().BeNull();
        snapshot.LimitWindows.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that switching to Stale returns a snapshot fetched approximately 20 minutes ago.
    /// </summary>
    [Fact]
    public async Task SetScenario_WhenStale_UpdatesSnapshotToStaleState()
    {

        var provider = new MockUsageProvider();

        provider.SetScenario(MockScenario.Stale);
        provider.CurrentScenario.Should().Be(MockScenario.Stale);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Stale);
        snapshot.FetchedAtUtc.Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(-19));
    }

    /// <summary>
    /// Verifies that Unidirectional scenario preserves null for TotalUnits and UsedFraction under Zero Fake Data.
    /// </summary>
    [Fact]
    public async Task SetScenario_WhenUnidirectional_EnforcesZeroFakeData()
    {

        var provider = new MockUsageProvider();

        provider.SetScenario(MockScenario.Unidirectional);
        provider.CurrentScenario.Should().Be(MockScenario.Unidirectional);

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Status.Should().Be(ProviderStatus.Ok);
        snapshot.LimitWindows.Should().ContainSingle();

        var window = snapshot.LimitWindows[0];
        window.RemainingUnits.Should().Be(450);
        window.TotalUnits.Should().BeNull();
        window.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that setting a custom snapshot overrides the active scenario and is returned faithfully.
    /// </summary>
    [Fact]
    public async Task SetCustomSnapshot_WhenValidSnapshotProvided_ReturnsCustomSnapshot()
    {

        var provider = new MockUsageProvider();
        var custom = new Snapshot
        {
            ProviderId = "mock",
            Status = ProviderStatus.AccessDenied,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ErrorDescription = "Insufficient permissions"
        };

        provider.SetCustomSnapshot(custom);

        provider.CurrentScenario.Should().BeNull();

        var snapshot = await provider.GetSnapshotAsync(TestContext.Current.CancellationToken);

        snapshot.Should().BeSameAs(custom);
    }

    /// <summary>
    /// Verifies that passing a null custom snapshot throws an <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void SetCustomSnapshot_WhenNull_ThrowsArgumentNullException()
    {

        var provider = new MockUsageProvider();

        var act = () => provider.SetCustomSnapshot(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that GetSnapshotAsync returns a canceled ValueTask when cancellation is requested.
    /// </summary>
    [Fact]
    public async Task GetSnapshotAsync_WhenCancellationRequested_ReturnsCanceledValueTask()
    {

        var provider = new MockUsageProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var valueTask = provider.GetSnapshotAsync(cts.Token);

        valueTask.IsCanceled.Should().BeTrue();

        var act = async () => await valueTask;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Verifies that an undefined scenario enum value throws an <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void SetScenario_WhenInvalidScenarioEnum_ThrowsArgumentOutOfRangeException()
    {

        var provider = new MockUsageProvider();

        var act = () => provider.SetScenario((MockScenario)999);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
