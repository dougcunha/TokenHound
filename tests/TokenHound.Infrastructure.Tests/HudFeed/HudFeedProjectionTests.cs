using AwesomeAssertions;
using System;
using System.Linq;
using TokenHound.App.HudFeed;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.HudFeed;

namespace TokenHound.Infrastructure.Tests.HudFeed;

/// <summary>Verifies that <see cref="HudFeedProjection"/> carries the HUD values and texts without inventing data.</summary>
public sealed class HudFeedProjectionTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 11, 7, 20, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies that the mock ring is left out and the other rings keep the HUD order.</summary>
    [Fact]
    public void Create_WhenMockRingPresent_DropsItAndKeepsOrder()
    {

        var rings = new[] { CreateRing("codex", 0.2), CreateRing("mock", 0.5), CreateRing("claude", 0.1) };

        var document = HudFeedProjection.Create(rings, FIXED_NOW, "1.2.3", TimeSpan.FromSeconds(30));

        document.Providers.Select(static p => p.ProviderId).Should().Equal("codex", "claude");
    }

    /// <summary>Verifies the document header fields.</summary>
    [Fact]
    public void Create_Always_WritesHeader()
    {

        var document = HudFeedProjection.Create([], FIXED_NOW, "1.2.3", TimeSpan.FromSeconds(29.5));

        document.SchemaVersion.Should().Be(HudFeedDocument.CURRENT_SCHEMA_VERSION);
        document.IsRunning.Should().BeTrue();
        document.AppVersion.Should().Be("1.2.3");
        document.WrittenAtUtc.Should().Be(FIXED_NOW);
        document.HeartbeatSeconds.Should().Be(30);
        document.Providers.Should().BeEmpty();
    }

    /// <summary>Verifies that ring values, activity, and rows reach the provider entry.</summary>
    [Fact]
    public void Create_WhenRingHasData_MapsHudValues()
    {

        var ring = CreateRing("claude", 0.63, 0.22);
        ring.UpdateActivity(new AgentSession
        {
            Pid = 42,
            StartTimeUtc = FIXED_NOW,
            State = AgentSessionState.Busy,
            LastActivityUtc = FIXED_NOW
        });

        var provider = HudFeedProjection.Create([ring], FIXED_NOW, "1", TimeSpan.FromSeconds(30)).Providers[0];

        provider.DisplayName.Should().Be(ring.ProviderName);
        provider.FamilyName.Should().Be("Claude");
        provider.Badge.Should().Be(ring.ProviderBadge);
        provider.Status.Should().Be("ok");
        provider.IsBusy.Should().BeTrue();
        provider.ActiveSessionText.Should().Be("PID 42 • Active");
        provider.SessionUsedFraction.Should().Be(ring.SessionUsedFraction);
        provider.WeeklyUsedFraction.Should().Be(ring.WeeklyUsedFraction);
        provider.Rows.Select(static r => r.Key).Should().Equal(ring.Rows.Select(static r => r.Key));
        provider.Rows.Select(static r => r.PrimaryText).Should().Equal(ring.Rows.Select(static r => r.PrimaryQuantityText));
        provider.Rows[0].ResetTimeUtc.Should().Be(ring.Rows[0].ResetTimeUtc);
    }

    /// <summary>Verifies that the status is camelCase and that the HUD status message is kept.</summary>
    [Fact]
    public void Create_WhenNeedsAuth_WritesCamelCaseStatusAndMessage()
    {

        var ring = new ProviderRingViewModel("codex");
        ring.UpdateFromSnapshot(CreateSnapshot("codex") with { Status = ProviderStatus.NeedsAuth, ErrorDescription = "Run codex login" });

        var provider = HudFeedProjection.Create([ring], FIXED_NOW, "1", TimeSpan.FromSeconds(30)).Providers[0];

        provider.Status.Should().Be("needsAuth");
        provider.StatusMessage.Should().Be("Run codex login");
    }

    /// <summary>Verifies that a window with only a remaining count keeps a null fraction.</summary>
    [Fact]
    public void Create_WhenWindowReportsOnlyRemaining_KeepsFractionNull()
    {

        var window = new LimitWindow { Name = "pro", RemainingUnits = 120 };
        var ring = new ProviderRingViewModel("perplexity");
        ring.UpdateFromSnapshot(CreateSnapshot("perplexity") with { LimitWindows = [window] });

        var provider = HudFeedProjection.Create([ring], FIXED_NOW, "1", TimeSpan.FromSeconds(30)).Providers[0];

        provider.SessionUsedFraction.Should().BeNull();
        provider.Rows[0].UsedFraction.Should().BeNull();
        provider.Rows[0].PrimaryText.Should().Be("~120 requests");
    }

    private static ProviderRingViewModel CreateRing(string providerId, params double[] fractions)
    {

        var ring = new ProviderRingViewModel(providerId);
        ring.UpdateFromSnapshot(CreateSnapshot(providerId, fractions));

        return ring;
    }

    private static Snapshot CreateSnapshot(string providerId, params double[] fractions)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                .. fractions.Select(static (f, i) => new LimitWindow
                {
                    Name = i == 0 ? "five_hour" : "seven_day",
                    UsedFraction = f,
                    ResetTimeUtc = FIXED_NOW.AddHours(i + 1)
                })
            ]
        };
}
