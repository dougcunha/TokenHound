using AwesomeAssertions;
using System;
using System.Collections.Generic;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies that malformed metadata and rows cannot produce a valid-looking balance.
/// </summary>
public sealed class CopilotCreditPolicySafetyTests
{
    private static readonly DateTimeOffset PERIOD_START = DateTimeOffset.Parse("2026-09-01T00:00:00+00:00");
    private static readonly DateTimeOffset PERIOD_END = DateTimeOffset.Parse("2026-10-01T00:00:00+00:00");

    /// <summary>
    /// Verifies an invalid row prevents balance derivation even when another row is valid.
    /// </summary>
    [Fact]
    public void Evaluate_WithInvalidRow_DoesNotDeriveBalance()
    {
        var request = CreateRequest() with
        {
            Items = [CreateItem(), null!],
            Allowance = CreateAllowance()
        };

        var result = CopilotCreditPolicy.Evaluate(request);

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.Remaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies null filter values are treated as incompatible data without throwing.
    /// </summary>
    [Fact]
    public void Evaluate_WithNullFilterValue_LeavesBalanceUnavailable()
    {
        var usageFilters = new Dictionary<string, string>
        {
            ["model"] = "model-a"
        };
        var allowanceFilters = new Dictionary<string, string>
        {
            ["model"] = null!
        };
        var request = CreateRequest() with
        {
            Coverage = CreateCoverage() with { Filters = usageFilters },
            Allowance = CreateAllowance() with { Filters = allowanceFilters }
        };

        var result = CopilotCreditPolicy.Evaluate(request);

        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies a negative quantity is rejected instead of being treated as a correction.
    /// </summary>
    [Fact]
    public void Evaluate_WithNegativeQuantity_ReturnsInvalidData()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(items: [CreateItem() with { GrossQuantity = -1m }]));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.GrossUsed.Should().BeNull();
    }

    /// <summary>
    /// Verifies allowance data without provenance is not used as a denominator.
    /// </summary>
    [Fact]
    public void Evaluate_WithUndocumentedAllowance_LeavesBalanceUnavailable()
    {
        var allowance = CreateAllowance() with { DocumentationUrl = "" };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: allowance));

        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
        result.Issues.Should().Contain("AllowanceEvidenceMissing");
    }

    /// <summary>
    /// Verifies an unverified period suppresses dependent balance values.
    /// </summary>
    [Fact]
    public void Evaluate_WithUnverifiedPeriod_SuppressesBalance()
    {
        var allowance = CreateAllowance() with
        {
            Period = CreatePeriod() with
            {
                IsVerified = false
            }
        };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: allowance));

        result.Usage.GrossUsed.Should().Be(725m);
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
    }

    /// <summary>
    /// Verifies negative allowance evidence is rejected.
    /// </summary>
    [Fact]
    public void Evaluate_WithNegativeAllowance_ReturnsInvalidData()
    {
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(allowance: CreateAllowance(-1m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.IncludedTotal.Should().BeNull();
    }

    /// <summary>
    /// Verifies undefined billing scopes are rejected as invalid metadata.
    /// </summary>
    [Fact]
    public void Evaluate_WithUndefinedScope_ReturnsInvalidData()
    {
        var context = CreateContext() with { Scope = (CopilotBillingScope)99 };
        var result = CopilotCreditPolicy.Evaluate(
            CreateRequest(context: context, allowance: CreateAllowance(5700m)));

        result.Outcome.Should().Be(CopilotCreditPolicyOutcome.InvalidData);
        result.Usage.IncludedTotal.Should().BeNull();
        result.Usage.Remaining.Should().BeNull();
    }

    private static CopilotCreditAggregationRequest CreateRequest(
        IReadOnlyList<CopilotCreditUsageItem>? items = null,
        CopilotBillingContext? context = null,
        CopilotAllowanceEvidence? allowance = null)
        => new()
        {
            Context = context ?? CreateContext(),
            Period = CreatePeriod(),
            Coverage = CreateCoverage(),
            Source = CopilotCreditSource.BillingApi,
            FetchedAtUtc = PERIOD_START.AddHours(1),
            Filter = new CopilotCreditFilter
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

    private static CopilotAllowanceEvidence CreateAllowance()
        => CreateAllowance(5700m);

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
