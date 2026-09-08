using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    /// <summary>
    /// Loads cached Copilot credit usage for the specified context, period, and filters.
    /// </summary>
    public CopilotCreditUsage? LoadCopilotBilling(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);

        return LoadCopilotBilling(
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth,
            filters
        );
    }

    /// <summary>
    /// Loads cached Copilot credit usage for the specified parameters.
    /// </summary>
    public CopilotCreditUsage? LoadCopilotBilling(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(principalId);
        var normalizedFilters = NormalizeFilters(filters);
        var entry = FindMatchingEntry(principalId, scope, ownerId, year, month, normalizedFilters);

        return entry?.Usage;
    }

    /// <summary>
    /// Loads cached Copilot daily summaries for the specified context, period, and filters.
    /// </summary>
    public IReadOnlyList<CopilotDailyUsageSummary> LoadCopilotDailySummaries(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters = null)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);
        var normalizedFilters = NormalizeFilters(filters);
        var entry = FindMatchingEntry(
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth,
            normalizedFilters
        );

        return entry?.DailySummaries ?? [];
    }

    /// <summary>
    /// Persists Copilot credit usage in copilot_billing.json using an atomic serialized write.
    /// </summary>
    public async Task SaveCopilotBillingAsync(
        CopilotCreditUsage usage,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(usage);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            var (doc, key, normalizedFilters) = PrepareBillingUpdate(usage.Context, usage.Period, usage.Coverage.Filters);
            var summaries = doc.Entries.TryGetValue(key, out var existing) ? existing.DailySummaries : [];

            doc.Entries[key] = CreateCacheEntry(usage.Context, usage.Period, normalizedFilters, usage, summaries);
            await WriteJsonAtomicallyAsync(CopilotBillingPath, doc, cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _writeLock.Release();
        }
    }

    /// <summary>
    /// Persists daily usage summaries in copilot_billing.json using an atomic serialized write.
    /// </summary>
    public async Task SaveCopilotDailySummariesAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyCollection<CopilotDailyUsageSummary> summaries,
        IReadOnlyDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
    {

        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(summaries);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            var (doc, key, normalizedFilters) = PrepareBillingUpdate(context, period, filters);
            var existingUsage = doc.Entries.TryGetValue(key, out var existing) ? existing.Usage : null;
            var currentSummaries = existing?.DailySummaries ?? [];
            var merged = MergeDailySummaries(currentSummaries, summaries);

            doc.Entries[key] = CreateCacheEntry(context, period, normalizedFilters, existingUsage, merged);
            await WriteJsonAtomicallyAsync(CopilotBillingPath, doc, cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _writeLock.Release();
        }
    }

    /// <summary>
    /// Persists a single day usage summary in copilot_billing.json using an atomic serialized write.
    /// </summary>
    public Task SaveCopilotDailySummaryAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        CopilotDailyUsageSummary summary,
        IReadOnlyDictionary<string, string>? filters = null,
        CancellationToken cancellationToken = default)
        => SaveCopilotDailySummariesAsync(
            context,
            period,
            [summary],
            filters,
            cancellationToken
        );

    private CopilotBillingCacheEntry? FindMatchingEntry(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        string normalizedFilters)
    {

        var doc = ReadBillingDocument();

        if (doc is null)
            return null;

        var key = BuildBillingCacheKey(principalId, scope, ownerId, year, month, normalizedFilters);

        if (!doc.Entries.TryGetValue(key, out var entry))
            return null;

        return MatchesContext(entry, principalId, scope, ownerId, year, month, normalizedFilters) ? entry : null;
    }

    private (CopilotBillingCacheDocument doc, string key, string normalizedFilters) PrepareBillingUpdate(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters)
    {

        var doc = ReadBillingDocumentForWrite();
        var normalizedFilters = NormalizeFilters(filters);
        var key = BuildBillingCacheKey(
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth,
            normalizedFilters
        );

        PruneOutdatedPeriods(
            doc,
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth
        );

        return (doc, key, normalizedFilters);
    }

    private static IReadOnlyList<CopilotDailyUsageSummary> MergeDailySummaries(
        IReadOnlyList<CopilotDailyUsageSummary> existing,
        IReadOnlyCollection<CopilotDailyUsageSummary> incoming)
    {

        var map = existing.ToDictionary(s => s.Day);

        foreach (var s in incoming)
            map[s.Day] = s;

        return map.Values.OrderBy(s => s.Day).ToList();
    }

    private static CopilotBillingCacheEntry CreateCacheEntry(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        string normalizedFilters,
        CopilotCreditUsage? usage,
        IReadOnlyList<CopilotDailyUsageSummary> summaries)
        => new()
        {
            PrincipalId = context.PrincipalId,
            Scope = context.Scope,
            OwnerId = context.OwnerId,
            Year = period.RequestedYear,
            Month = period.RequestedMonth,
            NormalizedFilters = normalizedFilters,
            Usage = usage,
            DailySummaries = summaries,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
}
