using System;
using AwesomeAssertions;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>Verifies that <see cref="ProviderBadgeResolver"/> maps monitoring state and snapshots into badge states, labels, and brush keys.</summary>
public sealed class ProviderBadgeResolverTests
{
    /// <summary>Verifies that every provider health status maps to the badge state of the same name.</summary>
    [Fact]
    public void ResolveState_WhenMonitoredWithSnapshot_MapsEveryProviderStatus()
    {

        foreach (var status in Enum.GetValues<ProviderStatus>())
        {
            var expected = Enum.Parse<ProviderBadgeState>(status.ToString());

            var state = ProviderBadgeResolver.ResolveState(true, CreateSnapshot(status));

            state.Should().Be(expected, "the badge state for {0} must not fall through to a default", status);
        }
    }

    /// <summary>Verifies that an unmonitored provider resolves to <see cref="ProviderBadgeState.Disabled"/> for any snapshot.</summary>
    [Fact]
    public void ResolveState_WhenNotMonitored_AlwaysResolvesDisabled()
    {

        ProviderBadgeResolver.ResolveState(false, null).Should().Be(ProviderBadgeState.Disabled);

        foreach (var status in Enum.GetValues<ProviderStatus>())
        {
            var state = ProviderBadgeResolver.ResolveState(false, CreateSnapshot(status));

            state.Should().Be(ProviderBadgeState.Disabled, "user deactivation outranks the reported {0} health", status);
        }
    }

    /// <summary>Verifies that a monitored provider without a snapshot shows the transient checking state.</summary>
    [Fact]
    public void ResolveState_WhenMonitoredWithoutSnapshot_ResolvesChecking()
    {

        var state = ProviderBadgeResolver.ResolveState(true, null);

        state.Should().Be(ProviderBadgeState.Checking);
    }

    /// <summary>Verifies that every badge state exposes a visible label and both pill brush keys.</summary>
    [Fact]
    public void ResolveLabelAndKeys_ForEveryBadgeState_ReturnsVisibleText()
    {

        foreach (var state in Enum.GetValues<ProviderBadgeState>())
        {
            ProviderBadgeResolver.ResolveLabel(state).Should().NotBeNullOrWhiteSpace();
            ProviderBadgeResolver.ResolveBackgroundKey(state).Should().NotBeNullOrWhiteSpace();
            ProviderBadgeResolver.ResolveForegroundKey(state).Should().NotBeNullOrWhiteSpace();
        }
    }

    /// <summary>Verifies the product wording of each badge label.</summary>
    /// <param name="state">The badge state under test.</param>
    /// <param name="expectedLabel">The label required by the product specification.</param>
    [Theory]
    [InlineData(ProviderBadgeState.Ok, "OK")]
    [InlineData(ProviderBadgeState.NeedsAuth, "Needs Auth")]
    [InlineData(ProviderBadgeState.RateLimited, "Rate Limited")]
    [InlineData(ProviderBadgeState.Stale, "Stale")]
    [InlineData(ProviderBadgeState.AccessDenied, "Access Denied")]
    [InlineData(ProviderBadgeState.Disabled, "Disabled")]
    [InlineData(ProviderBadgeState.Checking, "Checking...")]
    [InlineData(ProviderBadgeState.Unsupported, "No Quota")]
    public void ResolveLabel_WhenKnownState_ReturnsSpecifiedWording(ProviderBadgeState state, string expectedLabel)
    {

        ProviderBadgeResolver.ResolveLabel(state).Should().Be(expectedLabel);
    }

    /// <summary>Verifies that distinct badge states never share a label, so states stay distinguishable as text.</summary>
    [Fact]
    public void ResolveLabel_AcrossAllStates_ProducesDistinctLabels()
    {

        var labels = Array.ConvertAll(Enum.GetValues<ProviderBadgeState>(), ProviderBadgeResolver.ResolveLabel);

        labels.Should().OnlyHaveUniqueItems();
    }

    private static Snapshot CreateSnapshot(ProviderStatus status)
        => new()
        {
            ProviderId = "claude",
            Status = status,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UnixEpoch,
            LimitWindows = []
        };
}
