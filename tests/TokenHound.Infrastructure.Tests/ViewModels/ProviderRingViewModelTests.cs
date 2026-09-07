using AwesomeAssertions;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>Verifies that <see cref="ProviderRingViewModel"/> correctly resolves glyphs, badges, and names.</summary>
public sealed class ProviderRingViewModelTests
{
    /// <summary>Verifies that known providers resolve their respective vector mark resource keys.</summary>
    /// <param name="providerId">The identifier of the provider under test.</param>
    /// <param name="expectedGlyphKey">The expected resource key of the provider mark.</param>
    [Theory]
    [InlineData("claude", "Glyph.Claude")]
    [InlineData("gemini", "Glyph.Antigravity")]
    [InlineData("antigravity", "Glyph.Antigravity")]
    [InlineData("codex", "Glyph.Codex")]
    [InlineData("cursor", "Glyph.Cursor")]
    [InlineData("copilot", "Glyph.Copilot")]
    [InlineData("glm", "Glyph.Glm")]
    [InlineData("grok", "Glyph.Grok")]
    [InlineData("perplexity", "Glyph.Perplexity")]
    public void Constructor_WhenKnownProvider_ResolvesGlyphKey(string providerId, string expectedGlyphKey)
    {

        var ring = new ProviderRingViewModel(providerId);

        ring.GlyphKey.Should().Be(expectedGlyphKey);
        ring.GlyphScale.Should().BeGreaterThan(0.0);
    }

    /// <summary>Verifies that unknown or mock providers resolve null glyph keys to fall back to text badge.</summary>
    /// <param name="providerId">The provider identifier without an official mark.</param>
    [Theory]
    [InlineData("mock")]
    [InlineData("unknown-provider")]
    public void Constructor_WhenUnmappedProvider_ResolvesNullGlyphKey(string providerId)
    {

        var ring = new ProviderRingViewModel(providerId);

        ring.GlyphKey.Should().BeNull();
        ring.GlyphScale.Should().Be(1.0);
    }

    /// <summary>Verifies that passing a custom glyph key in the constructor overrides default resolution.</summary>
    [Fact]
    public void Constructor_WhenCustomGlyphKeyPassed_OverridesDefault()
    {

        const string customKey = "Glyph.Custom";
        var ring = new ProviderRingViewModel("mock", glyphKey: customKey);

        ring.GlyphKey.Should().Be(customKey);
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

    /// <summary>Verifies unsupported Copilot snapshots remain explicitly unavailable in the generic ring.</summary>
    [Fact]
    public void UpdateFromSnapshot_WhenUnsupportedPreservesUnavailableStatus()
    {
        var ring = new ProviderRingViewModel("copilot");
        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Unsupported,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ErrorDescription = "Copilot has no usable finite quota or entitlement."
        };

        ring.UpdateFromSnapshot(snapshot);

        ring.ProviderName.Should().Be("Copilot");
        ring.GlyphKey.Should().Be("Glyph.Copilot");
        ring.UsedFraction.Should().BeNull();
        ring.Status.Should().Be(ProviderStatus.Unsupported);
        ring.StatusMessage.Should().Be(snapshot.ErrorDescription);
    }

    /// <summary>Verifies generic status guidance surfaces both blocking and non-blocking quota blocks.</summary>
    [Fact]
    public void UpdateFromSnapshot_WhenActiveBlockExistsSurfacesItsReason()
    {
        var ring = new ProviderRingViewModel("copilot");
        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Monthly Premium Interactions",
                    UsedFraction = 1.0,
                    RemainingUnits = 0,
                    TotalUnits = 100,
                    ResetTimeUtc = DateTimeOffset.UtcNow.AddHours(1)
                }
            ],
            ActiveBlock = new UsageBlock
            {
                IsBlocked = false,
                Reason = "Quota exhausted; metered overage continues."
            }
        };

        ring.UpdateFromSnapshot(snapshot);

        ring.StatusMessage.Should().Be("Quota exhausted; metered overage continues.");
    }

    /// <summary>Verifies activity events update the existing generic ring activity properties.</summary>
    [Fact]
    public void UpdateActivity_WhenBusySessionProvidedSetsBusyAndDescription()
    {
        var ring = new ProviderRingViewModel("copilot");
        var session = new AgentSession
        {
            Pid = 42,
            StartTimeUtc = DateTimeOffset.UtcNow,
            State = AgentSessionState.Busy,
            LastActivityUtc = DateTimeOffset.UtcNow
        };

        ring.UpdateActivity(session);

        ring.IsBusy.Should().BeTrue();
        ring.ActiveSessionText.Should().Contain("PID 42");
    }
}
