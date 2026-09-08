using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

internal static class CopilotCreditPolicyEvaluator
{
    internal static CopilotCreditPolicy.Result Evaluate(CopilotCreditAggregationRequest request)
    {

        var issues = new List<string>();
        var metadata = CopilotCreditPolicyMetadata.Validate(request, issues);
        var selection = CopilotCreditPolicyAggregation.Select(request.Items, request.Filter, issues);
        var values = CopilotCreditPolicyAggregation.Aggregate(selection.Items, issues);
        var usage = CreateUsage(request, values);
        var invalid = metadata.IsInvalid || selection.IsInvalid || values.IsInvalid;
        var partial = metadata.IsPartial || values.HasMissingDimension;

        usage = ApplyAllowance(
            usage,
            request.Filter,
            metadata,
            issues,
            ref invalid,
            ref partial
        );

        return new CopilotCreditPolicy.Result
        {
            Usage = usage,
            Outcome = ResolveOutcome(selection.Items.Count, invalid, partial),
            Issues = issues
        };
    }

    private static CopilotCreditUsage CreateUsage(
        CopilotCreditAggregationRequest request,
        CopilotCreditPolicyAggregation.AggregationResult values)
    {

        var context = request.Context ?? CopilotCreditPolicyMetadata.CreateUnknownContext();
        var period = request.Period ?? new CopilotBillingPeriod();
        var coverage = request.Coverage ?? new CopilotReportCoverage();

        return new CopilotCreditUsage
        {
            Context = context,
            Period = period,
            Coverage = coverage,
            UnitType = request.Filter?.UnitType?.Trim(),
            GrossUsed = values.Gross,
            DiscountedUsed = values.Discount,
            NetUsed = values.Net,
            Allowance = request.Allowance,
            Source = request.Source,
            SourceAsOfUtc = request.SourceAsOfUtc,
            FetchedAtUtc = request.FetchedAtUtc,
            IsEstimated = request.IsEstimated || request.Source == CopilotCreditSource.DailyUserReport
        };
    }

    private static CopilotCreditUsage ApplyAllowance(
        CopilotCreditUsage usage,
        CopilotCreditFilter? filter,
        CopilotCreditPolicyMetadata.MetadataResult metadata,
        ICollection<string> issues,
        ref bool invalid,
        ref bool partial)
    {

        var allowance = usage.Allowance;

        if (allowance is null)
            return usage;

        var isAllowanceValid = IsAllowanceValid(
            usage,
            filter,
            allowance,
            issues,
            out var allowanceInvalid
        );

        if (!isAllowanceValid)
        {

            invalid |= allowanceInvalid;
            partial = true;

            return WithoutAllowance(usage);
        }

        var balance = CalculateBalance(
            usage,
            allowance,
            metadata,
            invalid,
            issues
        );

        invalid |= balance.IsInvalid;
        partial |= balance.IsPartial;

        return WithBalance(usage, allowance.Value, balance);
    }

    private static CopilotCreditUsage WithBalance(
        CopilotCreditUsage usage,
        decimal allowance,
        BalanceResult balance)
        => usage with
        {
            IncludedTotal = allowance,
            Remaining = balance.Remaining,
            UsedFraction = balance.Fraction
        };

    private static CopilotCreditUsage WithoutAllowance(CopilotCreditUsage usage)
        => usage with
        {
            Allowance = null,
            IncludedTotal = null,
            Remaining = null,
            UsedFraction = null
        };

    private static bool IsAllowanceValid(
        CopilotCreditUsage usage,
        CopilotCreditFilter? filter,
        CopilotAllowanceEvidence allowance,
        ICollection<string> issues,
        out bool isInvalid)
    {

        isInvalid = false;

        var metadataValid = ValidateAllowanceMetadata(
            usage,
            allowance,
            issues,
            ref isInvalid
        );

        if (!ValidateAllowanceValue(allowance, issues, ref isInvalid) ||
            !ValidateAllowanceEvidence(allowance, issues, ref isInvalid) ||
            !metadataValid)
            return false;

        if (!CopilotCreditPolicyMetadata.ContextMatches(usage.Context, allowance.Context) ||
            !CopilotCreditPolicyMetadata.PeriodMatches(usage.Period, allowance.Period) ||
            !CopilotCreditPolicyMetadata.IdentifierMatches(usage.UnitType, allowance.Unit) ||
            !CopilotCreditPolicyMetadata.FiltersMatch(usage.Coverage.Filters, allowance.Filters) ||
            !CopilotCreditPolicyMetadata.AreRequestedFiltersCovered(filter, allowance.Filters))
        {

            issues.Add("AllowanceIncompatible");

            return false;
        }

        return true;
    }

    private static bool ValidateAllowanceValue(
        CopilotAllowanceEvidence allowance,
        ICollection<string> issues,
        ref bool isInvalid)
    {

        if (allowance.Value >= 0)
            return true;

        issues.Add("AllowanceNegative");
        isInvalid = true;

        return false;
    }

    private static bool ValidateAllowanceEvidence(
        CopilotAllowanceEvidence allowance,
        ICollection<string> issues,
        ref bool isInvalid)
    {

        if (CopilotCreditPolicyMetadata.HasAllowanceEvidence(allowance))
            return true;

        issues.Add("AllowanceEvidenceMissing");
        isInvalid = true;

        return false;
    }

    private static bool ValidateAllowanceMetadata(
        CopilotCreditUsage usage,
        CopilotAllowanceEvidence allowance,
        ICollection<string> issues,
        ref bool isInvalid)
    {

        if (CopilotCreditPolicyMetadata.IsContextValid(usage.Context) &&
            CopilotCreditPolicyMetadata.IsPeriodComplete(usage.Period) &&
            CopilotCreditPolicyMetadata.IsContextValid(allowance.Context) &&
            CopilotCreditPolicyMetadata.IsPeriodComplete(allowance.Period))
            return true;

        issues.Add("AllowanceMetadataInvalid");
        isInvalid = true;

        return false;
    }

    private static BalanceResult CalculateBalance(
        CopilotCreditUsage usage,
        CopilotAllowanceEvidence allowance,
        CopilotCreditPolicyMetadata.MetadataResult metadata,
        bool inputInvalid,
        ICollection<string> issues)
    {

        if (inputInvalid || metadata.IsPartial || usage.GrossUsed is null ||
            !CopilotCreditPolicyMetadata.CoverageMatchesPeriod(usage.Coverage, usage.Period))
        {

            issues.Add("BalanceCoverageIncomplete");

            return new BalanceResult(
                null,
                null,
                false,
                true
            );
        }

        try
        {

            checked
            {

                var remaining = allowance.Value - usage.GrossUsed.Value;
                decimal? fraction = allowance.Value > 0 ? usage.GrossUsed.Value / allowance.Value : null;

                return new BalanceResult(
                    remaining,
                    fraction,
                    false,
                    false
                );
            }
        }
        catch (OverflowException)
        {

            issues.Add("RemainingOverflow");

            return new BalanceResult(
                null,
                null,
                true,
                false
            );
        }
    }

    private static CopilotCreditPolicyOutcome ResolveOutcome(
        int compatibleItemCount,
        bool invalid,
        bool partial)
    {

        if (invalid)
            return CopilotCreditPolicyOutcome.InvalidData;

        if (compatibleItemCount == 0)
            return CopilotCreditPolicyOutcome.UnsupportedData;

        return partial ? CopilotCreditPolicyOutcome.Partial : CopilotCreditPolicyOutcome.Valid;
    }

    private readonly record struct BalanceResult(
        decimal? Remaining,
        decimal? Fraction,
        bool IsInvalid,
        bool IsPartial);
}
