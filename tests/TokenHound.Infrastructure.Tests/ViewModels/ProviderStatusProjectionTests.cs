using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies grouping, dynamic columns, status reuse, and exhausted accounts of <see cref="ProviderStatusProjection"/>.
/// </summary>
public sealed class ProviderStatusProjectionTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 26, 22, 23, 0, TimeSpan.Zero);

    /// <summary>Verifies that Claude profiles share one group with the default profile first, ordered by name.</summary>
    [Fact]
    public void Group_WhenClaudeProfilesAndOtherProvider_GroupsByFamily()
    {

        var formatter = CreateFormatter();
        var accounts = new[] { "codex", "claude-work", "claude" }
            .Select(id => ProviderStatusProjection.CreateAccount(id, CreateSnapshot(id, 0.1), formatter));

        var groups = ProviderStatusProjection.Group(accounts);

        groups.Select(static g => g.FamilyName).Should().Equal("Claude", "Codex");
        groups[0].Count.Should().Be(2);
        groups[0].Accounts.Select(static a => a.DisplayName).Should().Equal("Claude Code", "Claude Code (work)");
        groups[1].Count.Should().Be(1);
    }

    /// <summary>Verifies that columns mirror the HUD usage rows in count, order, key, and label.</summary>
    [Fact]
    public void CreateAccount_WhenSnapshotHasWindows_ColumnsMirrorHudRows()
    {

        var formatter = CreateFormatter();
        var snapshot = CreateSnapshot("claude", 0.63, 0.22, 1.0);

        var account = ProviderStatusProjection.CreateAccount("claude", snapshot, formatter);
        var rows = ProviderUsageRowFactory.CreateRows(snapshot, formatter.TimeProvider);

        account.Columns.Select(static c => c.Key).Should().Equal(rows.Select(static r => r.Key));
        account.Columns.Select(static c => c.Label).Should().Equal(rows.Select(static r => r.Label));
        account.Columns[0].ValueText.Should().Be("63%");
        account.Columns[0].Level.Should().Be(UsageLevel.Yellow);
        account.Columns[0].HasBar.Should().BeTrue();
    }

    /// <summary>Verifies that a window without a used fraction shows its quantity text and no bar.</summary>
    [Fact]
    public void CreateAccount_WhenWindowHasNoFraction_ShowsQuantityWithoutBar()
    {

        var snapshot = CreateSnapshot("codex") with
        {
            LimitWindows = [new LimitWindow { Name = "requests", RemainingUnits = 12 }]
        };

        var column = ProviderStatusProjection.CreateAccount("codex", snapshot, CreateFormatter()).Columns.Single();

        column.ValueText.Should().Be("~12 requests");
        column.HasBar.Should().BeFalse();
        column.Level.Should().Be(UsageLevel.None);
        column.ResetText.Should().Be("No reset pending");
    }

    /// <summary>Verifies that Copilot credit columns mirror the HUD rows and show quantity text without a bar.</summary>
    [Fact]
    public void CreateAccount_WhenCopilotCredits_ColumnsMirrorHudRows()
    {

        var formatter = CreateFormatter();
        var snapshot = CreateCopilotSnapshot() with
        {
            LimitWindows = [CreateWindow("premium_interactions", 0.3, FIXED_NOW.AddDays(5))]
        };

        var account = ProviderStatusProjection.CreateAccount("copilot", snapshot, formatter);
        var rows = ProviderUsageRowFactory.CreateRows(snapshot, formatter.TimeProvider);

        account.Columns.Select(static c => c.Key).Should().Equal(rows.Select(static r => r.Key));
        account.Columns.Select(static c => c.Label).Should().Equal(rows.Select(static r => r.Label));
        var credits = account.Columns.Single(static c => c.Key == "copilot:credits");
        credits.ValueText.Should().Be("725 credits used");
        credits.HasBar.Should().BeFalse();
        credits.ResetText.Should().Be("in 22 days · 10/18, 22:23");
    }

    /// <summary>Verifies that Cline columns mirror the HUD rows, including the free model limit reset.</summary>
    [Fact]
    public void CreateAccount_WhenClineFreeLimit_ColumnsMirrorHudRows()
    {

        var formatter = CreateFormatter();
        var snapshot = CreateSnapshot("cline") with
        {
            Status = ProviderStatus.RateLimited,
            ClineAccount = new ClineAccountUsage { BalanceCredits = 12.5, HasPassSubscription = true },
            ActiveBlock = new UsageBlock
            {
                Reason = "FreeModelLimitReached",
                IsBlocked = true,
                ResetTimeUtc = FIXED_NOW.AddMinutes(90)
            }
        };

        var account = ProviderStatusProjection.CreateAccount("cline", snapshot, formatter);
        var rows = ProviderUsageRowFactory.CreateRows(snapshot, formatter.TimeProvider);

        account.Columns.Select(static c => c.Key).Should().Equal(rows.Select(static r => r.Key));
        account.Columns.Select(static c => c.Label).Should().Equal(rows.Select(static r => r.Label));
        var limit = account.Columns.Single(static c => c.Key == "cline:freelimit");
        limit.ValueText.Should().Be("Limit reached");
        limit.HasBar.Should().BeFalse();
        limit.ResetTimeUtc.Should().Be(FIXED_NOW.AddMinutes(90));
        limit.ResetText.Should().Be("in 1 hour · 09/26, 23:53");
    }

    /// <summary>Verifies that the account status reuses the HUD status message.</summary>
    [Fact]
    public void CreateAccount_WhenNeedsAuth_ReusesHudStatusMessage()
    {

        var snapshot = CreateSnapshot("claude") with { Status = ProviderStatus.NeedsAuth };

        var account = ProviderStatusProjection.CreateAccount("claude", snapshot, CreateFormatter());

        account.StatusMessage.Should().Be(ProviderRingViewModel.ResolveStatusMessage(snapshot));
        account.StatusMessage.Should().Be("Execute 'claude login' in terminal");
        account.IsBlocked.Should().BeFalse();
    }

    /// <summary>Verifies that the HUD error statuses mark the account as blocked, and other statuses do not.</summary>
    /// <param name="status">The snapshot status.</param>
    /// <param name="expected">Whether the account is expected to be blocked.</param>
    [Theory]
    [InlineData(ProviderStatus.RateLimited, true)]
    [InlineData(ProviderStatus.AccessDenied, true)]
    [InlineData(ProviderStatus.Ok, false)]
    [InlineData(ProviderStatus.NeedsAuth, false)]
    [InlineData(ProviderStatus.Stale, false)]
    public void CreateAccount_ByStatus_MarksBlockedLikeTheHud(ProviderStatus status, bool expected)
    {

        var snapshot = CreateSnapshot("codex", 0.4) with { Status = status };

        var account = ProviderStatusProjection.CreateAccount("codex", snapshot, CreateFormatter());

        account.IsBlocked.Should().Be(expected);
    }

    /// <summary>Verifies that an active block marks the account as blocked only while it blocks requests.</summary>
    /// <param name="isBlocked">The active block flag.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CreateAccount_WithActiveBlock_FollowsItsBlockedFlag(bool isBlocked)
    {

        var snapshot = CreateSnapshot("cline") with
        {
            ActiveBlock = new UsageBlock { Reason = "FreeModelLimitReached", IsBlocked = isBlocked }
        };

        var account = ProviderStatusProjection.CreateAccount("cline", snapshot, CreateFormatter());

        account.IsBlocked.Should().Be(isBlocked);
    }

    /// <summary>Verifies that an exhausted account counts down to the earliest reset of its exhausted windows.</summary>
    [Fact]
    public void CreateAccount_WhenWindowsExhausted_ShowsEarliestComeback()
    {

        var snapshot = CreateSnapshot("claude") with
        {
            LimitWindows =
            [
                CreateWindow("seven_day", 1.0, FIXED_NOW.AddDays(2)),
                CreateWindow("five_hour", 0.4, FIXED_NOW.AddMinutes(30)),
                CreateWindow("seven_day_opus", 1.0, FIXED_NOW.AddMinutes(877))
            ]
        };

        var account = ProviderStatusProjection.CreateAccount("claude", snapshot, CreateFormatter());

        account.IsExhausted.Should().BeTrue();
        account.BackInText.Should().Be("back in 14h 37m");
        account.Columns.Count(static c => c.IsExhausted).Should().Be(2);
    }

    /// <summary>Verifies that an exhausted window without a reset keeps the account dimmed without a countdown.</summary>
    [Fact]
    public void CreateAccount_WhenExhaustedWithoutReset_ShowsLimitReached()
    {

        var snapshot = CreateSnapshot("claude") with { LimitWindows = [CreateWindow("seven_day", 1.0, null)] };

        var account = ProviderStatusProjection.CreateAccount("claude", snapshot, CreateFormatter());

        account.IsExhausted.Should().BeTrue();
        account.BackInText.Should().Be("limit reached");
    }

    /// <summary>Verifies the pending account of an enabled provider without a snapshot.</summary>
    [Fact]
    public void CreateAccount_WhenNoSnapshot_ReturnsPendingAccount()
    {

        var account = ProviderStatusProjection.CreateAccount("cursor", null, CreateFormatter());

        account.IsPending.Should().BeTrue();
        account.StatusMessage.Should().Be("Waiting for first reading");
        account.Columns.Should().BeEmpty();
        account.IsExhausted.Should().BeFalse();
        account.IsBlocked.Should().BeFalse();
    }

    private static ProviderStatusFormatter CreateFormatter()
        => new(new ManualTimeProvider(FIXED_NOW), CultureInfo.InvariantCulture);

    private static Snapshot CreateSnapshot(string providerId, params double[] fractions)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows = [.. fractions.Select(static (f, i) => CreateWindow($"window_{i}", f, FIXED_NOW.AddHours(i + 1)))]
        };

    private static Snapshot CreateCopilotSnapshot()
        => CreateSnapshot("copilot") with
        {
            CopilotBilling = new CopilotBillingStatus
            {
                State = CopilotBillingState.Available,
                Reason = CopilotBillingReason.None,
                AttemptedAtUtc = FIXED_NOW,
                Usage = new CopilotCreditUsage
                {
                    Context = new CopilotBillingContext
                    {
                        PrincipalId = "user-1",
                        Scope = CopilotBillingScope.Organization,
                        OwnerName = "ColibriAgile",
                        Plan = CopilotPlanType.Business
                    },
                    Period = new CopilotBillingPeriod
                    {
                        RequestedYear = 2026,
                        RequestedMonth = 9,
                        IsVerified = true,
                        ResetUtc = FIXED_NOW.AddDays(22)
                    },
                    Coverage = new CopilotReportCoverage
                    {
                        IsComplete = true,
                        MissingDays = [],
                        Filters = new Dictionary<string, string>(),
                        HasMissingPartitions = false,
                        HasInvalidRows = false
                    },
                    GrossUsed = 725m,
                    NetUsed = 725m,
                    Source = CopilotCreditSource.BillingApi,
                    FetchedAtUtc = FIXED_NOW,
                    IsEstimated = false
                }
            }
        };

    private static LimitWindow CreateWindow(string name, double usedFraction, DateTimeOffset? resetTimeUtc)
        => new()
        {
            Name = name,
            UsedFraction = usedFraction,
            ResetTimeUtc = resetTimeUtc
        };
}
