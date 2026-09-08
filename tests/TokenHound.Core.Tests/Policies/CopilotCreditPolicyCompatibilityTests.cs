using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies coverage, compatibility, and evidence changes that guard Copilot balances.
/// </summary>
public sealed class CopilotCreditPolicyCompatibilityTests
{
    private static readonly DateTimeOffset PERIOD_START = DateTimeOffset.Parse("2026-09-01T00:00:00+00:00");
    private static readonly DateTimeOffset PERIOD_END = DateTimeOffset.Parse("2026-10-01T00:00:00+00:00");

    /// <summary>
    /// Verifies partial flags, missing partitions, and missing days suppress a balance.
    /// </summary>
    [Fact]
    public void Evaluate_WithIncompleteCoverageMarkers_SuppressesBalance()
    {
        var coverage = CreateCoverage() with
        {
            IsComplete = false,
            MissingDays = [new DateOnly(2026, 9, 12)],
            HasMissingPartitions = true,
            HasInvalidRows = true
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                allowance: CreateAllowance(5700m),
                coverage: coverage));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Partial);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().Be(5700m);
        result.Usage.Remaining.Should().BeNull();
        result.Usage.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies an allowance for another billing owner cannot supply a denominator.
    /// </summary>
    [Fact]
    public void Evaluate_WithIncompatibleOwner_DoesNotApplyAllowance()
    {
        var allowance = CreateAllowance(5700m) with
        {
            Context = CreateContext() with { OwnerId = "owner-2" }
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: allowance));

        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
        result.Issues.Should().Contain("AllowanceIncompatible");
    }

    /// <summary>
    /// Verifies a different requested unit cannot combine with a credit allowance.
    /// </summary>
    [Fact]
    public void Evaluate_WithIncompatibleUnit_DoesNotApplyAllowance()
    {
        var filter = new CopilotCreditFilter
        {
            Product = "copilot-ai",
            UnitType = "requests"
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                allowance: CreateAllowance(5700m),
                filter: filter));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.UnsupportedData);
        result.Usage.GrossUsed.Should().BeNull();
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies an allowance from another verified period cannot be reused.
    /// </summary>
    [Fact]
    public void Evaluate_WithIncompatiblePeriod_DoesNotApplyAllowance()
    {
        var priorPeriod = CreatePeriod() with
        {
            StartUtc = PERIOD_START.AddMonths(-1),
            EndExclusiveUtc = PERIOD_START,
            RequestedMonth = 8
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                allowance: CreateAllowance(5700m) with { Period = priorPeriod }));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Partial);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
        result.Issues.Should().Contain("AllowanceIncompatible");
    }

    /// <summary>
    /// Verifies positive-allowance overage preserves a negative balance and ratio above one.
    /// </summary>
    [Fact]
    public void Evaluate_WithPositiveAllowanceOverage_PreservesSignedBalance()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: CreateAllowance(100m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Valid);
        result.Usage.IncludedTotal.Should().Be(100m);
        result.Usage.Remaining.Should().Be(-625m);
        result.Usage.UsedFraction.Should().Be(7.25m);
    }

    /// <summary>
    /// Verifies an updated authoritative allowance is used directly across plan changes.
    /// </summary>
    [Fact]
    public void Evaluate_WithUpdatedPromotionalAllowance_UsesEvidenceValueWithoutPlanMath()
    {
        var context = CreateContext() with { Plan = CopilotPlanType.Business };
        var original = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                context: context,
                allowance: CreateAllowance(5700m) with { Context = context }));
        var updated = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                context: context,
                allowance: CreateAllowance(6300m) with
                {
                    Context = context,
                    EvidenceKey = "allowance-promotion-seat-change"
                }));

        original.Usage.IncludedTotal.Should().Be(5700m);
        updated.Usage.IncludedTotal.Should().Be(6300m);
        updated.Usage.Remaining.Should().Be(5575m);
    }

    /// <summary>
    /// Verifies a model subset cannot use an allowance that covers the full pool.
    /// </summary>
    [Fact]
    public void Evaluate_WithModelSubset_DoesNotUseFullPoolAllowance()
    {
        var filter = new CopilotCreditFilter
        {
            Product = "copilot-ai",
            UnitType = "credits",
            Model = "model-a"
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(
                allowance: CreateAllowance(5700m),
                filter: filter));

        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
        result.Issues.Should().Contain("AllowanceIncompatible");
    }

    private static CopilotCreditAggregationRequest CreateRequest(
        IReadOnlyList<CopilotCreditUsageItem>? items = null,
        CopilotBillingContext? context = null,
        CopilotAllowanceEvidence? allowance = null,
        CopilotReportCoverage? coverage = null,
        CopilotCreditFilter? filter = null)
        => new()
        {
            Context = context ?? CreateContext(),
            Period = CreatePeriod(),
            Coverage = coverage ?? CreateCoverage(),
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = PERIOD_START.AddHours(1),
            Filter = filter ?? new CopilotCreditFilter
            {
                Product = "copilot-ai",
                UnitType = "credits"
            },
            Items = items ?? [CreateItem()],
            Allowance = allowance
        };

    private static CopilotBillingContext CreateContext()
        => new()
        {
            PrincipalId = "user-1",
            Scope = CopilotBillingScope.Personal,
            OwnerId = "user-1",
            Plan = CopilotPlanType.Pro,
            EvidenceKey = "owner-proof-1"
        };

    private static CopilotBillingPeriod CreatePeriod()
        => new()
        {
            StartUtc = PERIOD_START,
            EndExclusiveUtc = PERIOD_END,
            RequestedYear = 2026,
            RequestedMonth = 9,
            IsVerified = true
        };

    private static CopilotReportCoverage CreateCoverage()
        => new()
        {
            StartDay = new DateOnly(2026, 9, 1),
            EndDay = new DateOnly(2026, 9, 30),
            IsComplete = true
        };

    private static CopilotAllowanceEvidence CreateAllowance(decimal value)
        => new()
        {
            JsonPath = "$.included_credits",
            DocumentationUrl = "https://docs.github.com/copilot/billing",
            EvidenceKey = "allowance-proof-1",
            Unit = "credits",
            Context = CreateContext(),
            Period = CreatePeriod(),
            Value = value
        };

    private static CopilotCreditUsageItem CreateItem()
        => new()
        {
            Product = "copilot-ai",
            Sku = "sku-a",
            Model = "model-a",
            UnitType = "credits",
            GrossQuantity = 725m,
            DiscountQuantity = 700m,
            NetQuantity = 25m
        };
}
