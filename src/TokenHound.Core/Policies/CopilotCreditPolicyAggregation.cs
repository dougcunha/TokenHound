using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

internal static class CopilotCreditPolicyAggregation
{
    internal static SelectionResult Select(
        IReadOnlyList<CopilotCreditUsageItem>? items,
        CopilotCreditFilter? filter,
        ICollection<string> issues)
    {

        if (items is null || filter is null)
            return new SelectionResult([], false);

        var compatible = new List<CopilotCreditUsageItem>();
        var invalid = false;

        foreach (var item in items)
        {

            if (item is null)
            {

                issues.Add("ItemInvalid");
                invalid = true;

                continue;
            }

            if (MatchesFilter(item, filter))
                compatible.Add(item);
        }

        return new SelectionResult(compatible, invalid);
    }

    internal static AggregationResult Aggregate(
        IReadOnlyList<CopilotCreditUsageItem> items,
        ICollection<string> issues)
    {

        if (items.Count == 0)
            return new AggregationResult(
                null,
                null,
                null,
                false,
                false
            );

        var gross = SumDimension(
            items,
            static item => item.GrossQuantity,
            "Gross",
            issues
        );
        var discount = SumDimension(
            items,
            static item => item.DiscountQuantity,
            "Discount",
            issues
        );
        var net = SumDimension(
            items,
            static item => item.NetQuantity,
            "Net",
            issues
        );

        return new AggregationResult(
            gross.Value,
            discount.Value,
            net.Value,
            gross.IsInvalid || discount.IsInvalid || net.IsInvalid,
            gross.IsMissing || discount.IsMissing || net.IsMissing
        );
    }

    private static DimensionResult SumDimension(
        IReadOnlyList<CopilotCreditUsageItem> items,
        Func<CopilotCreditUsageItem, decimal?> selector,
        string dimension,
        ICollection<string> issues)
    {

        var total = 0m;
        var missing = false;

        foreach (var item in items)
        {

            var quantity = selector(item);

            if (quantity is null)
            {

                missing = true;

                continue;
            }

            var added = TryAddQuantity(
                quantity.Value,
                dimension,
                issues,
                ref total
            );

            if (!added)
                return new DimensionResult(null, true, false);
        }

        if (missing)
            return MissingDimension(dimension, issues);

        return new DimensionResult(total, false, false);
    }

    private static DimensionResult MissingDimension(
        string dimension,
        ICollection<string> issues)
    {

        issues.Add($"{dimension}Missing");

        return new DimensionResult(null, false, true);
    }

    private static bool TryAddQuantity(
        decimal quantity,
        string dimension,
        ICollection<string> issues,
        ref decimal total)
    {

        if (quantity < 0)
        {

            issues.Add($"{dimension}Negative");

            return false;
        }

        try
        {

            checked
            {

                total += quantity;
            }
        }
        catch (OverflowException)
        {

            issues.Add($"{dimension}Overflow");

            return false;
        }

        return true;
    }

    private static bool MatchesFilter(CopilotCreditUsageItem item, CopilotCreditFilter filter)
        => CopilotCreditPolicyMetadata.IdentifierMatches(item.Product, filter.Product) &&
            CopilotCreditPolicyMetadata.IdentifierMatches(item.UnitType, filter.UnitType) &&
            OptionalIdentifierMatches(item.Sku, filter.Sku) &&
            OptionalIdentifierMatches(item.Model, filter.Model);

    private static bool OptionalIdentifierMatches(string? actual, string? expected)
        => expected is null || CopilotCreditPolicyMetadata.IdentifierMatches(actual, expected);

    internal readonly record struct SelectionResult(
        IReadOnlyList<CopilotCreditUsageItem> Items,
        bool IsInvalid);

    internal readonly record struct AggregationResult(
        decimal? Gross,
        decimal? Discount,
        decimal? Net,
        bool IsInvalid,
        bool HasMissingDimension);

    private readonly record struct DimensionResult(
        decimal? Value,
        bool IsInvalid,
        bool IsMissing);
}
