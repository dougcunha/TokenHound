using AwesomeAssertions;
using System;
using System.Linq;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies Claude quota breakdown rows and canonical ring summaries built from provider window names.
/// </summary>
public sealed class ClaudeQuotaPresentationTests
{
    private static readonly DateTimeOffset NOW = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies /usage-style labels with the reported model in the label, a visible 0%, and no fabricated secondary amount or reset.
    /// </summary>
    /// <param name="providerId">The default or named Claude profile identifier.</param>
    [Theory]
    [InlineData("claude")]
    [InlineData("claude-work")]
    public void Rows_LabelEveryClaudeQuotaDistinctly(string providerId)
    {

        var snapshot = CreateSnapshot(
            providerId,
            Window("five_hour", 0.35, NOW.AddHours(2)),
            Window("seven_day", 0.72),
            Window("seven_day_opus", 0.24),
            Window("seven_day_sonnet", 0),
            Window("weekly_scoped", 0.1) with { GroupName = "Fable" },
            Window("weekly_scoped", 0.2)
        );

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(NOW));

        rows.Select(static row => row.Label).Should().Equal(
            "Current session",
            "Current week (all models)",
            "Current week (Opus)",
            "Current week (Sonnet)",
            "Current week (Fable)",
            "Current week (scoped)"
        );
        rows[3].PrimaryQuantityText.Should().Be("0% Used");
        rows.Should().OnlyContain(static row => row.ScopeText == null);
        rows[0].ResetText.Should().Be("Resets in 2h 0m");
        rows.Skip(1).Should().OnlyContain(static row => row.ResetText == null);
        rows.Should().OnlyContain(static row => row.SecondaryQuantityText == null);
    }

    /// <summary>
    /// Verifies that an unknown reported suffix is humanized without guessing a model identity.
    /// </summary>
    [Fact]
    public void Rows_HumanizeUnknownSuffixWithoutGuessingAModel()
    {

        var snapshot = CreateSnapshot("claude", Window("seven_day_claude_design", 0.4));

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(NOW));

        rows.Should().ContainSingle().Which.Label.Should().Be("Current week (Claude design)");
    }

    /// <summary>
    /// Verifies that unrecognized Claude kinds keep neutral labels instead of borrowing base-window labels.
    /// </summary>
    [Fact]
    public void Rows_KeepUnrecognizedKindsNeutral()
    {

        var snapshot = CreateSnapshot(
            "claude-work",
            Window("session_opus", 0.2),
            Window("seven_hour_pool", 0.3)
        );

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, new FixedTimeProvider(NOW));

        rows.Select(static row => row.Label).Should().Equal("Session opus", "Seven hour pool");
    }

    /// <summary>
    /// Verifies that additional quotas never become Claude's session or overall weekly summary.
    /// </summary>
    [Fact]
    public void Ring_WithOnlyAdditionalQuotas_LeavesBaseSummariesUnmeasured()
    {

        var ring = new ProviderRingViewModel("claude-work");

        ring.UpdateFromSnapshot(
            CreateSnapshot("claude-work", Window("seven_day_opus", 0.9), Window("weekly_scoped", 0.8)),
            new FixedTimeProvider(NOW)
        );

        ring.UsedFraction.Should().BeNull();
        ring.SessionUsedFraction.Should().BeNull();
        ring.WeeklyUsedFraction.Should().BeNull();
        ring.Rows.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies exact canonical selection even when an additional weekly quota is listed first.
    /// </summary>
    [Fact]
    public void Ring_SelectsExactCanonicalWindows()
    {

        var ring = new ProviderRingViewModel("claude");

        ring.UpdateFromSnapshot(
            CreateSnapshot(
                "claude",
                Window("seven_day_opus", 0.9),
                Window("five_hour", 0.35),
                Window("seven_day", 0.72)
            ),
            new FixedTimeProvider(NOW)
        );

        ring.UsedFraction.Should().Be(0.35);
        ring.SessionUsedFraction.Should().Be(0.35);
        ring.WeeklyUsedFraction.Should().Be(0.72);
    }

    /// <summary>
    /// Verifies that non-Claude providers keep the generic label and positional ring fallback.
    /// </summary>
    [Fact]
    public void NonClaudeProvider_KeepsGenericSelectionAndLabels()
    {

        var snapshot = CreateSnapshot("codex", Window("seven_day_opus", 0.9), Window("Monthly", 0.2));
        var ring = new ProviderRingViewModel("codex");

        ring.UpdateFromSnapshot(snapshot, new FixedTimeProvider(NOW));

        ring.Rows[0].Label.Should().Be("Weekly limit (7d)");
        ring.SessionUsedFraction.Should().Be(0.9);
        ring.WeeklyUsedFraction.Should().Be(0.9);
    }

    private static LimitWindow Window(string name, double fraction, DateTimeOffset? reset = null)
        => new() { Name = name, UsedFraction = fraction, ResetTimeUtc = reset };

    private static Snapshot CreateSnapshot(string providerId, params LimitWindow[] windows)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = NOW,
            LimitWindows = windows
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
            => utcNow;
    }
}
