using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies compatible Copilot credit aggregation and evidence-gated balances.
/// </summary>
public sealed class CopilotCreditPolicyTests
{
    private static readonly DateTimeOffset PERIOD_START = DateTimeOffset.Parse("2026-09-01T00:00:00+00:00");
    private static readonly DateTimeOffset PERIOD_END = DateTimeOffset.Parse("2026-10-01T00:00:00+00:00");

    /// <summary>
    /// Verifies the documented 5,700 allowance and 725 usage fixture.
    /// </summary>
    [Fact]
    public void Evaluate_WithVerifiedAllowance_ProducesRemainingAndFraction()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: CreateAllowance(5700m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Valid);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.DiscountedUsed.Should().Be(700m);
        result.Usage.NetUsed.Should().Be(25m);
        result.Usage.IncludedTotal.Should().Be(5700m);
        result.Usage.Remaining.Should().Be(4975m);
        result.Usage.UsedFraction.Should().Be(725m / 5700m);
    }

    /// <summary>
    /// Verifies that usage without approved allowance evidence remains consumption-only.
    /// </summary>
    [Fact]
    public void Evaluate_WithoutAllowance_KeepsBalanceUnavailable()
    {
        var usage = CopilotCreditPolicy.Evaluate(CreateRequest()).Usage;

        usage.GrossUsed.Should().Be(725m);
        usage.IncludedTotal.Should().BeNull();
        usage.Remaining.Should().BeNull();
        usage.UsedFraction.Should().BeNull();
        usage.Allowance.Should().BeNull();
    }

    /// <summary>
    /// Verifies that each quantity dimension is aggregated independently.
    /// </summary>
    [Fact]
    public void Evaluate_WithMissingDimension_PreservesOtherDimensions()
    {
        var request = CreateRequest(
            items:
            [
                CreateItem(1.25m, null, 3.5m),
                CreateItem(2.75m, 4.25m, 1.5m)
            ]);

        var result = CopilotCreditPolicy.Evaluate(request);

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Partial);
        result.Usage.GrossUsed.Should().Be(4.00m);
        result.Usage.DiscountedUsed.Should().BeNull();
        result.Usage.NetUsed.Should().Be(5.0m);
    }

    /// <summary>
    /// Verifies that punctuation differences do not make distinct identifiers collide.
    /// </summary>
    [Fact]
    public void Evaluate_WithPunctuationDifference_UsesOnlyExactIdentifier()
    {
        var request = CreateRequest(
            items:
            [
                CreateItem(10m, product: "copilotai"),
                CreateItem(7m, product: "COPILOT-AI")
            ]);

        var result = CopilotCreditPolicy.Evaluate(request);

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Valid);
        result.Usage.GrossUsed.Should().Be(7m);
    }

    /// <summary>
    /// Verifies that an empty response cannot silently become a zero reading.
    /// </summary>
    [Fact]
    public void Evaluate_WithNoCompatibleItems_LeavesQuantitiesUnavailable()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(items: Array.Empty<CopilotCreditUsageItem>()));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.UnsupportedData);
        result.Usage.GrossUsed.Should().BeNull();
        result.Usage.DiscountedUsed.Should().BeNull();
        result.Usage.NetUsed.Should().BeNull();
    }

    /// <summary>
    /// Verifies that explicitly reported zero dimensions remain valid zero values.
    /// </summary>
    [Fact]
    public void Evaluate_WithExplicitZeroItem_PreservesZeroQuantities()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(items: [CreateItem(0m, 0m, 0m)]));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Valid);
        result.Usage.GrossUsed.Should().Be(0m);
        result.Usage.DiscountedUsed.Should().Be(0m);
        result.Usage.NetUsed.Should().Be(0m);
    }

    /// <summary>
    /// Verifies that checked decimal aggregation reports overflow as invalid data.
    /// </summary>
    [Fact]
    public void Evaluate_WhenDimensionOverflows_ReturnsInvalidData()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(items: [CreateItem(decimal.MaxValue), CreateItem(1m)]));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.GrossUsed.Should().BeNull();
        result.Issues.Should().Contain("GrossOverflow");
    }

    /// <summary>
    /// Verifies that personal context requires stable owner and evidence identifiers.
    /// </summary>
    [Fact]
    public void Evaluate_WithUnverifiedPersonalContext_DoesNotApplyAllowance()
    {
        var context = CreateContext() with { OwnerId = null, EvidenceKey = null };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(context: context, allowance: CreateAllowance(5700m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a coverage interval outside the billing period suppresses balances.
    /// </summary>
    [Fact]
    public void Evaluate_WhenCoverageDoesNotMatchPeriod_SuppressesDerivedBalance()
    {
        var coverage = CreateCoverage() with { StartDay = new DateOnly(2026, 9, 2) };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(coverage: coverage, allowance: CreateAllowance(5700m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.Partial);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().Be(5700m);
        result.Usage.Remaining.Should().BeNull();
        result.Usage.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that historical report sources retain estimated qualification.
    /// </summary>
    [Fact]
    public void Evaluate_WithDailyReport_IsAlwaysEstimated()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(source: CopilotCreditSource.DailyUserReport));

        result.Usage.IsEstimated.Should().BeTrue();
    }

    /// <summary>
    /// Verifies signed overage and the null fraction for a zero allowance.
    /// </summary>
    [Fact]
    public void Evaluate_WithZeroAllowance_PreservesSignedOverageAndNoFraction()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: CreateAllowance(0m)));

        result.Usage.IncludedTotal.Should().Be(0m);
        result.Usage.Remaining.Should().Be(-725m);
        result.Usage.UsedFraction.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an incompatible allowance owner cannot supply a denominator.
    /// </summary>
    [Fact]
    public void Evaluate_WithUnprovenOrIncompatibleAllowance_LeavesBalanceUnavailable()
    {
        var narrowed = CopilotCreditPolicy.Evaluate(
            CreateRequest(model: "model-a", allowance: CreateAllowance(5700m)));
        narrowed.Usage.IncludedTotal.Should().BeNull();

        var allowance = CreateAllowance(5700m) with
        {
            Context = CreateContext() with { OwnerId = "another-user" }
        };

        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: allowance));

        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
        result.Issues.Should().Contain("AllowanceIncompatible");
    }

    private static CopilotCreditAggregationRequest CreateRequest(
        IReadOnlyList<CopilotCreditUsageItem>? items = null,
        CopilotAllowanceEvidence? allowance = null,
        CopilotBillingContext? context = null,
        CopilotBillingPeriod? period = null,
        CopilotReportCoverage? coverage = null,
        CopilotCreditSource source = CopilotCreditSource.BillingApi,
        string? model = null)
        => new()
        {
            Context = context ?? CreateContext(),
            Period = period ?? CreatePeriod(),
            Coverage = coverage ?? CreateCoverage(),
            Source = source,
            FetchedAtUtc = PERIOD_START.AddHours(1),
            Filter = new CopilotCreditFilter
            {
                Product = "copilot-ai",
                UnitType = "credits",
                Model = model
            },
            Items = items ?? [CreateItem(725m, 700m, 25m)],
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

    private static CopilotCreditUsageItem CreateItem(
        decimal? gross,
        decimal? discount = 0m,
        decimal? net = 0m,
        string product = "copilot-ai")
        => new()
        {
            Product = product,
            Sku = "sku-a",
            Model = "model-a",
            UnitType = "credits",
            GrossQuantity = gross,
            DiscountQuantity = discount,
            NetQuantity = net
        };
}
