using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies that <see cref="ProviderUsageRowFactory"/> correctly projects quota windows into presentation rows.
/// </summary>
public sealed class ProviderUsageRowFactoryTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that standard session and weekly quota windows are projected with correct labels and percentages.
    /// </summary>
    [Fact]
    public void CreateRows_WhenStandardQuotaWindows_ProjectsCorrectLabelsAndQuantities()
    {

        var timeProvider = new FixedTimeProvider(FIXED_NOW);
        var snapshot = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "five_hour",
                    Period = TimeSpan.FromHours(5),
                    UsedFraction = 0.42,
                    RemainingUnits = 58,
                    TotalUnits = 100,
                    ResetTimeUtc = FIXED_NOW.AddHours(2).AddMinutes(15)
                },
                new LimitWindow
                {
                    Name = "seven_day",
                    Period = TimeSpan.FromDays(7),
                    UsedFraction = 0.15,
                    RemainingUnits = 850,
                    TotalUnits = 1000,
                    ResetTimeUtc = FIXED_NOW.AddDays(3).AddHours(4)
                }
            ]
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, timeProvider);

        rows.Should().HaveCount(2);

        var sessionRow = rows[0];
        sessionRow.Key.Should().Be("quota:0");
        sessionRow.Label.Should().Be("Current session (5h)");
        sessionRow.UsedFraction.Should().Be(0.42);
        sessionRow.ClampedFraction.Should().Be(0.42);
        sessionRow.HasProgress.Should().BeTrue();
        sessionRow.PrimaryQuantityText.Should().Be("42% Used");
        sessionRow.SecondaryQuantityText.Should().Be("58 of 100 remaining");
        sessionRow.ResetText.Should().Be("Resets in 2h 15m");

        var weeklyRow = rows[1];
        weeklyRow.Key.Should().Be("quota:1");
        weeklyRow.Label.Should().Be("Weekly limit (7d)");
        weeklyRow.UsedFraction.Should().Be(0.15);
        weeklyRow.PrimaryQuantityText.Should().Be("15% Used");
        weeklyRow.SecondaryQuantityText.Should().Be("850 of 1,000 remaining");
        weeklyRow.ResetText.Should().Be("Resets in 3d 4h");
    }

    /// <summary>
    /// Verifies that overage utilization clamps progress fill to 1.0 while preserving exact displayed percentage.
    /// </summary>
    [Fact]
    public void CreateRows_WhenOverage_ClampsFillWhilePreservingLabel()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "claude",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Session",
                    UsedFraction = 1.25,
                    RemainingUnits = 0,
                    TotalUnits = 100
                }
            ]
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        rows.Should().ContainSingle();
        rows[0].UsedFraction.Should().Be(1.25);
        rows[0].ClampedFraction.Should().Be(1.0);
        rows[0].PrimaryQuantityText.Should().Be("125% Used");
    }

    /// <summary>
    /// Verifies that derived snapshots with request counts omit progress bar and display request counts.
    /// </summary>
    [Fact]
    public void CreateRows_WhenDerivedRequestsWindow_DisplaysRequestsCountWithoutProgress()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "antigravity",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Requests Today",
                    Period = TimeSpan.FromDays(1),
                    RemainingUnits = 42,
                    TotalUnits = null,
                    UsedFraction = null
                }
            ]
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        rows.Should().ContainSingle();
        var row = rows[0];
        row.UsedFraction.Should().BeNull();
        row.HasProgress.Should().BeFalse();
        row.PrimaryQuantityText.Should().Be("~42 requests");
        row.SecondaryQuantityText.Should().Be("No limit published");
    }

    /// <summary>
    /// Verifies that a provider-defined quota group is shown as the row scope.
    /// </summary>
    [Fact]
    public void CreateRows_WhenQuotaWindowHasGroupName_ExposesGroupAsScope()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "gemini",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Weekly Limit",
                    GroupName = "Gemini models",
                    Period = TimeSpan.FromDays(7),
                    UsedFraction = 0.42,
                    TotalUnits = 100
                }
            ]
        };

        var row = ProviderUsageRowFactory.CreateRows(snapshot).Should().ContainSingle().Subject;

        row.ScopeText.Should().Be("Gemini models");
    }

    /// <summary>
    /// Verifies that Copilot finite quota window is labeled as Premium interactions while non-premium retains its name.
    /// </summary>
    [Fact]
    public void CreateRows_WhenCopilotQuota_ResolvesPremiumInteractionsOrCategoryName()
    {

        var snapshot = new Snapshot
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Monthly Premium Interactions",
                    UsedFraction = 0.50
                },
                new LimitWindow
                {
                    Name = "Chat Interactions",
                    UsedFraction = 0.10
                }
            ]
        };

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        rows.Should().HaveCount(2);
        rows[0].Label.Should().Be("Premium interactions");
        rows[1].Label.Should().Be("Chat Interactions");
    }

    /// <summary>
    /// Verifies that reset countdown formatting handles past, minute, hour, and day ranges correctly.
    /// </summary>
    [Theory]
    [InlineData(0, "Resets now")]
    [InlineData(-10, "Resets now")]
    [InlineData(45, "Resets in 45m")]
    [InlineData(135, "Resets in 2h 15m")]
    [InlineData(2900, "Resets in 2d 0h")]
    public void FormatResetCountdown_FormatsExpectedCountdownString(int minutesOffset, string expected)
    {

        var resetTime = FIXED_NOW.AddMinutes(minutesOffset);
        var formatted = ProviderUsageRowFactory.FormatResetCountdown(resetTime, FIXED_NOW);

        formatted.Should().Be(expected);
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {

            _now = now;
        }

        public override DateTimeOffset GetUtcNow()
            => _now;
    }
}
