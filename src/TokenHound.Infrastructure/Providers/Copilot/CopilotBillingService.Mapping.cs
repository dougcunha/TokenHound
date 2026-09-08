using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotBillingService
{
    private CopilotBillingStatus CreateStatus(
        CopilotBillingReason reason,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc)
        => new()
        {
            State = cachedUsage is not null ? CopilotBillingState.Stale : CopilotBillingState.Unavailable,
            Reason = reason,
            Usage = cachedUsage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = null
        };

    private CopilotBillingStatus CreateRateLimitedStatus(
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc)
        => new()
        {
            State = cachedUsage is not null ? CopilotBillingState.Stale : CopilotBillingState.Unavailable,
            Reason = CopilotBillingReason.RateLimited,
            Usage = cachedUsage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = _gate.ActiveDeadlineUtc
        };

    private CopilotBillingStatus HandleUnresolvedScope(
        CopilotBillingContext context,
        DateTimeOffset nowUtc)
    {

        var reason = string.Equals(context.EvidenceKey, "ambiguous", StringComparison.OrdinalIgnoreCase)
            ? CopilotBillingReason.AmbiguousScope
            : CopilotBillingReason.UnknownScope;

        return new CopilotBillingStatus
        {
            State = CopilotBillingState.Unavailable,
            Reason = reason,
            Usage = null,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = _gate.ActiveDeadlineUtc
        };
    }

    private static IReadOnlyList<CopilotCreditUsageItem> ExtractCreditItems(
        IReadOnlyList<CopilotBillingUsageItem>? items)
    {

        if (items is null)
            return [];

        return items
            .Where(static i => string.Equals(i.Product, "Copilot", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(i.Sku, "Copilot AI Credits", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(i.UnitType, "ai-credits", StringComparison.OrdinalIgnoreCase))
            .Select(static i => new CopilotCreditUsageItem
            {
                Product = i.Product,
                Sku = i.Sku,
                Model = i.Model,
                UnitType = i.UnitType,
                GrossQuantity = i.GrossQuantity,
                DiscountQuantity = i.DiscountQuantity,
                NetQuantity = i.NetQuantity
            })
            .ToList();
    }

    private static CopilotBillingPeriod CreateBillingPeriod(DateTimeOffset nowUtc)
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

    private static CopilotReportCoverage CreateCoverage(CopilotBillingPeriod period)
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

    private static IReadOnlyList<DateOnly> GetCandidateDaysForPeriod(
        CopilotBillingPeriod period,
        DateTimeOffset nowUtc)
    {

        var start = new DateOnly(period.RequestedYear, period.RequestedMonth, 1);
        var daysInMonth = DateTime.DaysInMonth(period.RequestedYear, period.RequestedMonth);
        var lastDay = new DateOnly(period.RequestedYear, period.RequestedMonth, daysInMonth);

        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        var end = (today.Year == period.RequestedYear && today.Month == period.RequestedMonth)
            ? today
            : lastDay;

        if (end < start)
            return [start];

        var days = new List<DateOnly>();

        for (var d = start; d <= end; d = d.AddDays(1))
            days.Add(d);

        return days;
    }

    private static CopilotCreditUsage BuildHistoricalUsage(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyList<CopilotDailyUsageSummary> summaries,
        IReadOnlyList<DateOnly> candidateDays,
        DateTimeOffset nowUtc)
    {

        var grossUsed = summaries.Sum(static s => s.AiCreditsUsed);
        var coveredDays = summaries.Select(static s => s.Day).ToHashSet();
        var missingDays = candidateDays.Where(d => !coveredDays.Contains(d)).OrderBy(static d => d).ToList();
        var hasMissingPartitions = summaries.Any(static s => s.HasMissingPartitions);
        var hasInvalidRows = summaries.Any(static s => s.HasInvalidRows);

        var coverage = new CopilotReportCoverage
        {
            StartDay = candidateDays.Count > 0 ? candidateDays[0] : null,
            EndDay = candidateDays.Count > 0 ? candidateDays[^1] : null,
            MissingDays = missingDays,
            HasMissingPartitions = hasMissingPartitions,
            HasInvalidRows = hasInvalidRows,
            IsComplete = missingDays.Count == 0 && !hasMissingPartitions && !hasInvalidRows,
            Filters = new Dictionary<string, string>()
        };

        var items = new List<CopilotCreditUsageItem>
        {
            new()
            {
                Product = "Copilot",
                Sku = "Copilot AI Credits",
                UnitType = "ai-credits",
                GrossQuantity = grossUsed,
                DiscountQuantity = null,
                NetQuantity = null
            }
        };

        var request = new CopilotCreditAggregationRequest
        {
            Context = context,
            Period = period,
            Coverage = coverage,
            Source = CopilotCreditSource.DailyUserReport,
            SourceAsOfUtc = summaries.Count > 0 ? summaries.Max(static s => s.FetchedAtUtc) : null,
            FetchedAtUtc = nowUtc,
            IsEstimated = true,
            Filter = new CopilotCreditFilter
            {
                Product = "Copilot",
                Sku = "Copilot AI Credits",
                UnitType = "ai-credits"
            },
            Items = items,
            Allowance = null
        };

        var evaluation = CopilotCreditPolicy.Evaluate(request);

        return evaluation.Usage;
    }
}
