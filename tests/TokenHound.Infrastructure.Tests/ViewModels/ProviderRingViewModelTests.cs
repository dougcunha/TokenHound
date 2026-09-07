using AwesomeAssertions;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>Verifies that <see cref="ProviderRingViewModel"/> correctly resolves logos, badges, and names.</summary>
public sealed class ProviderRingViewModelTests
{
    /// <summary>Verifies that known providers resolve their respective pack URI logo sources.</summary>
    /// <param name="providerId">The identifier of the provider under test.</param>
    /// <param name="expectedLogoName">The expected PNG asset name in the pack URI.</param>
    [Theory]
    [InlineData("claude", "claude.png")]
    [InlineData("gemini", "antigravity.png")]
    [InlineData("antigravity", "antigravity.png")]
    [InlineData("codex", "codex.png")]
    [InlineData("cursor", "cursor.png")]
    [InlineData("copilot", "copilot.png")]
    [InlineData("glm", "glm.png")]
    [InlineData("grok", "grok.png")]
    [InlineData("perplexity", "perplexity.png")]
    public void Constructor_WhenKnownProvider_ResolvesLogoSource(string providerId, string expectedLogoName)
    {

        var ring = new ProviderRingViewModel(providerId);

        ring.LogoSource.Should().NotBeNull();
        ring.LogoSource.Should().Contain(expectedLogoName);
    }

    /// <summary>Verifies that unknown or mock providers resolve null logo sources to fall back to text badge.</summary>
    /// <param name="providerId">The provider identifier without an official logo.</param>
    [Theory]
    [InlineData("mock")]
    [InlineData("unknown-provider")]
    public void Constructor_WhenUnmappedProvider_ResolvesNullLogoSource(string providerId)
    {

        var ring = new ProviderRingViewModel(providerId);

        ring.LogoSource.Should().BeNull();
    }

    /// <summary>Verifies that passing a custom logo source in the constructor overrides default resolution.</summary>
    [Fact]
    public void Constructor_WhenCustomLogoSourcePassed_OverridesDefault()
    {

        const string customUri = "pack://application:,,,/Custom/logo.png";
        var ring = new ProviderRingViewModel("mock", logoSource: customUri);

        ring.LogoSource.Should().Be(customUri);
    }

    /// <summary>Verifies default badge fallbacks for mapped and unmapped providers.</summary>
    [Fact]
    public void Constructor_ResolvesDefaultBadges()
    {

        var mockRing = new ProviderRingViewModel("mock");
        mockRing.ProviderBadge.Should().Be("M");

        var claudeRing = new ProviderRingViewModel("claude");
        claudeRing.ProviderBadge.Should().Be("C");

        var unknownRing = new ProviderRingViewModel("custom");
        unknownRing.ProviderBadge.Should().Be("C");
    }

    /// <summary>Verifies that derived snapshots with request counts update the status message and reset text.</summary>
    [Fact]
    public void UpdateFromSnapshot_WhenDerivedRequestsWindow_SetsSessionResetTextAndStatusMessage()
    {

        var ring = new ProviderRingViewModel("antigravity");
        var snapshot = new Snapshot
        {
            ProviderId = "antigravity",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Requests Today",
                    Period = TimeSpan.FromDays(1),
                    RemainingUnits = 42,
                    TotalUnits = null,
                },
            ],
        };

        ring.UpdateFromSnapshot(snapshot);

        ring.Status.Should().Be(ProviderStatus.Ok);
        ring.UsedFraction.Should().BeNull();
        ring.SessionResetText.Should().Be("~42 requests");
        ring.StatusMessage.Should().Be("~42 requests today · no limit published");
    }
}
