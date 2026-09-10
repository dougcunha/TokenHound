using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

internal static class CopilotBillingMapper
{
    internal static IReadOnlyList<CopilotCreditUsageItem> ExtractCreditItems(
        IReadOnlyList<CopilotBillingUsageItem>? items)
    {

        if (items is null)
            return [];

        return items
            .Where(static item => string.Equals(item.Product, "Copilot", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.Sku, "Copilot AI Credits", StringComparison.OrdinalIgnoreCase)
                && string.Equals(item.UnitType, "ai-credits", StringComparison.OrdinalIgnoreCase))
            .Select(static item => new CopilotCreditUsageItem
            {
                Product = item.Product,
                Sku = item.Sku,
                Model = item.Model,
                UnitType = item.UnitType,
                GrossQuantity = item.GrossQuantity,
                DiscountQuantity = item.DiscountQuantity,
                NetQuantity = item.NetQuantity
            })
            .ToList();
    }

    internal static CopilotBillingPeriod CreatePeriod(DateTimeOffset nowUtc)
    {

        var startUtc = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);

        return new CopilotBillingPeriod
        {
            RequestedYear = nowUtc.Year,
            RequestedMonth = nowUtc.Month,
            StartUtc = startUtc,
            EndExclusiveUtc = startUtc.AddMonths(1),
            IsVerified = true
        };
    }

    internal static CopilotReportCoverage CreateCoverage(CopilotBillingPeriod period)
    {

        var daysInMonth = DateTime.DaysInMonth(period.RequestedYear, period.RequestedMonth);

        return new CopilotReportCoverage
        {
            IsComplete = true,
            StartDay = new DateOnly(period.RequestedYear, period.RequestedMonth, 1),
            EndDay = new DateOnly(period.RequestedYear, period.RequestedMonth, daysInMonth),
            MissingDays = [],
            Filters = new Dictionary<string, string>()
        };
    }

    internal static CopilotCreditAggregationRequest CreateDirectRequest(
        CopilotBillingPass pass,
        IReadOnlyList<CopilotBillingUsageItem>? items)
        => new()
        {
            Context = pass.Context,
            Period = pass.Period,
            Coverage = CreateCoverage(pass.Period),
            Source = CopilotCreditSource.BillingApi,
            SourceAsOfUtc = null,
            FetchedAtUtc = pass.NowUtc,
            IsEstimated = false,
            Filter = new CopilotCreditFilter
            {
                Product = "Copilot",
                Sku = "Copilot AI Credits",
                UnitType = "ai-credits"
            },
            Items = ExtractCreditItems(items),
            Allowance = null
        };

    internal static IReadOnlyList<DateOnly> GetCandidateDays(
        CopilotBillingPeriod period,
        DateTimeOffset nowUtc)
    {

        var start = new DateOnly(period.RequestedYear, period.RequestedMonth, 1);
        var daysInMonth = DateTime.DaysInMonth(period.RequestedYear, period.RequestedMonth);
        var lastDay = new DateOnly(period.RequestedYear, period.RequestedMonth, daysInMonth);
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var end = today.Year == period.RequestedYear && today.Month == period.RequestedMonth
            ? today
            : lastDay;

        if (end < start)
            return [start];

        var days = new List<DateOnly>();

        for (var day = start; day <= end; day = day.AddDays(1))
            days.Add(day);

        return days;
    }

    internal static CopilotCreditUsage BuildHistoricalUsage(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyList<CopilotDailyUsageSummary> summaries,
        IReadOnlyList<DateOnly> candidateDays,
        DateTimeOffset nowUtc)
    {

        var coverage = CreateHistoricalCoverage(summaries, candidateDays);
        var request = CreateHistoricalRequest(context, period, summaries, coverage, nowUtc);

        return CopilotCreditPolicy.Evaluate(request).Usage;
    }

    private static CopilotReportCoverage CreateHistoricalCoverage(
        IReadOnlyList<CopilotDailyUsageSummary> summaries,
        IReadOnlyList<DateOnly> candidateDays)
    {

        var coveredDays = summaries.Select(static summary => summary.Day).ToHashSet();
        var missingDays = candidateDays.Where(day => !coveredDays.Contains(day)).OrderBy(static day => day).ToList();
        var hasMissingPartitions = summaries.Any(static summary => summary.HasMissingPartitions);
        var hasInvalidRows = summaries.Any(static summary => summary.HasInvalidRows);

        return new CopilotReportCoverage
        {
            StartDay = candidateDays.Count > 0 ? candidateDays[0] : null,
            EndDay = candidateDays.Count > 0 ? candidateDays[^1] : null,
            MissingDays = missingDays,
            HasMissingPartitions = hasMissingPartitions,
            HasInvalidRows = hasInvalidRows,
            IsComplete = missingDays.Count == 0 && !hasMissingPartitions && !hasInvalidRows,
            Filters = new Dictionary<string, string>()
        };
    }

    private static CopilotCreditAggregationRequest CreateHistoricalRequest(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyList<CopilotDailyUsageSummary> summaries,
        CopilotReportCoverage coverage,
        DateTimeOffset nowUtc)
        => new()
        {
            Context = context,
            Period = period,
            Coverage = coverage,
            Source = CopilotCreditSource.DailyUserReport,
            SourceAsOfUtc = summaries.Count > 0 ? summaries.Max(static summary => summary.FetchedAtUtc) : null,
            FetchedAtUtc = nowUtc,
            IsEstimated = true,
            Filter = CreateHistoricalFilter(),
            Items = [CreateHistoricalItem(summaries)],
            Allowance = null
        };

    private static CopilotCreditFilter CreateHistoricalFilter()
        => new()
        {
            Product = "Copilot",
            Sku = "Copilot AI Credits",
            UnitType = "ai-credits"
        };

    private static CopilotCreditUsageItem CreateHistoricalItem(
        IReadOnlyList<CopilotDailyUsageSummary> summaries)
        => new()
        {
            Product = "Copilot",
            Sku = "Copilot AI Credits",
            UnitType = "ai-credits",
            GrossQuantity = summaries.Sum(static summary => summary.AiCreditsUsed),
            DiscountQuantity = null,
            NetQuantity = null
        };
}
