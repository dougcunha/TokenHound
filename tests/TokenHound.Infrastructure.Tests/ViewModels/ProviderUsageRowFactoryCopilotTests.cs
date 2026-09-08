using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.App.ViewModels;
using TokenHound.Core.Models;
using Xunit;

namespace TokenHound.Infrastructure.Tests.ViewModels;

/// <summary>
/// Verifies that <see cref="ProviderUsageRowFactory"/> correctly projects Copilot AI credit billing states.
/// </summary>
public sealed class ProviderUsageRowFactoryCopilotTests
{
    private static readonly DateTimeOffset FIXED_NOW = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that verified allowance and usage produces remaining balance, total, and direct billing provenance.
    /// </summary>
    [Fact]
    public void CreateRows_WhenCopilotWithVerifiedAllowance_ProjectsRemainingAndProvenance()
    {

        var timeProvider = new FixedTimeProvider(FIXED_NOW);
        var snapshot = CreateCopilotSnapshot(new CopilotCreditUsage
        {
            Context = new CopilotBillingContext
            {
                PrincipalId = "user-1",
                Scope = CopilotBillingScope.Organization,
                OwnerId = "org-1",
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
            IncludedTotal = 5700m,
            Remaining = 4975m,
            UsedFraction = 725m / 5700m,
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = FIXED_NOW,
            IsEstimated = false
        });

        var rows = ProviderUsageRowFactory.CreateRows(snapshot, timeProvider);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.Label.Should().Be("AI credits");
        creditRow.PrimaryQuantityText.Should().Be("4,975 remaining");
        creditRow.SecondaryQuantityText.Should().Be("725 of 5,700 total");
        creditRow.ScopeText.Should().Be("ColibriAgile (Organization)");
        creditRow.ResetText.Should().Be("Resets in 22d 0h");
        creditRow.ProvenanceText.Should().Be("Direct billing");
        creditRow.HasProgress.Should().BeTrue();
        creditRow.UsedFraction.Should().BeApproximately((double)(725m / 5700m), 0.0001);
    }

    /// <summary>
    /// Verifies that usage-only credit data without allowance produces no percentage, no 0/0, and unmeasured progress.
    /// </summary>
    [Fact]
    public void CreateRows_WhenCopilotUsageOnly_ProducesNoPercentageOrProgressFill()
    {

        var snapshot = CreateCopilotSnapshot(new CopilotCreditUsage
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
                ResetUtc = null
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
            IncludedTotal = null,
            Remaining = null,
            UsedFraction = null,
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = FIXED_NOW,
            IsEstimated = false
        });

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.UsedFraction.Should().BeNull();
        creditRow.HasProgress.Should().BeFalse();
        creditRow.PrimaryQuantityText.Should().Be("725 credits used");
        creditRow.SecondaryQuantityText.Should().BeNull();
        creditRow.ResetText.Should().BeNull();
        creditRow.ProvenanceText.Should().Be("Direct billing");
    }

    /// <summary>
    /// Verifies that daily user reports produce estimated historical provenance with partial coverage information.
    /// </summary>
    [Fact]
    public void CreateRows_WhenHistoricalDailyReportFallback_SurfacesEstimatedAndPartialCoverage()
    {

        var missingDay = new DateOnly(2026, 9, 5);
        var asOf = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.Zero);
        var snapshot = CreateCopilotSnapshot(new CopilotCreditUsage
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
                IsVerified = false,
                ResetUtc = null
            },
            Coverage = new CopilotReportCoverage
            {
                IsComplete = false,
                MissingDays = [missingDay],
                Filters = new Dictionary<string, string>(),
                HasMissingPartitions = false,
                HasInvalidRows = false
            },
            GrossUsed = 312.5m,
            NetUsed = 312.5m,
            Source = CopilotCreditSource.DailyUserReport,
            SourceAsOfUtc = asOf,
            FetchedAtUtc = FIXED_NOW,
            IsEstimated = true
        });

        var rows = ProviderUsageRowFactory.CreateRows(snapshot);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.PrimaryQuantityText.Should().Be("312.5 credits used");
        creditRow.ProvenanceText.Should().Be("Daily report · Estimated · Partial (1d missing) · As of 2026-09-07");
    }

    /// <summary>
    /// Verifies that missing reset on billing does not borrow from operational quota reset.
    /// </summary>
    [Fact]
    public void CreateRows_WhenMissingBillingReset_DoesNotBorrowFromQuotaWindow()
    {

        var snapshot = CreateCopilotSnapshot(new CopilotCreditUsage
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
                ResetUtc = null
            },
            Coverage = new CopilotReportCoverage
            {
                IsComplete = true,
                MissingDays = [],
                Filters = new Dictionary<string, string>(),
                HasMissingPartitions = false,
                HasInvalidRows = false
            },
            GrossUsed = 100m,
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = FIXED_NOW,
            IsEstimated = false
        });

        snapshot = new Snapshot
        {
            ProviderId = snapshot.ProviderId,
            Status = snapshot.Status,
            Fidelity = snapshot.Fidelity,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Monthly Premium Interactions",
                    ResetTimeUtc = FIXED_NOW.AddDays(5)
                }
            ],
            CopilotBilling = snapshot.CopilotBilling
        };

        var timeProvider = new FixedTimeProvider(FIXED_NOW);
        var rows = ProviderUsageRowFactory.CreateRows(snapshot, timeProvider);

        var creditRow = rows.Should().ContainSingle(r => r.Key == "copilot:credits").Subject;
        creditRow.ResetText.Should().BeNull();

        var quotaRow = rows.Should().ContainSingle(r => r.Key == "quota:0").Subject;
        quotaRow.ResetText.Should().Be("Resets in 5d 0h");
    }

    private static Snapshot CreateCopilotSnapshot(CopilotCreditUsage usage)
        => new()
        {
            ProviderId = "copilot",
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = FIXED_NOW,
            LimitWindows = [],
            CopilotBilling = new CopilotBillingStatus
            {
                State = CopilotBillingState.Available,
                Reason = CopilotBillingReason.None,
                AttemptedAtUtc = FIXED_NOW,
                Usage = usage
            }
        };

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
