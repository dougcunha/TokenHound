using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

internal sealed partial class CopilotBillingArchive
{
    private const int SCHEMA_VERSION = 1;

    private static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _directoryPath;
    private readonly string _billingPath;
    private readonly SemaphoreSlim _writeLock;

    internal CopilotBillingArchive(
        string directoryPath,
        string billingPath,
        SemaphoreSlim writeLock)
    {

        _directoryPath = directoryPath;
        _billingPath = billingPath;
        _writeLock = writeLock;
    }

    internal CopilotCreditUsage? Load(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters)
        => Load(
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth,
            filters
        );

    internal CopilotCreditUsage? Load(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        IReadOnlyDictionary<string, string>? filters)
    {

        var normalizedFilters = NormalizeFilters(filters);
        var entry = FindMatchingEntry(principalId, scope, ownerId, year, month, normalizedFilters);

        return entry?.Usage;
    }

    internal IReadOnlyList<CopilotDailyUsageSummary> LoadDailySummaries(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters)
    {

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

    internal async Task SaveAsync(
        CopilotCreditUsage usage,
        CancellationToken cancellationToken)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var (document, key, normalizedFilters) = PrepareUpdate(
                usage.Context,
                usage.Period,
                usage.Coverage.Filters
            );
            var summaries = GetExistingDailySummaries(document, key);

            document.Entries[key] = CreateEntry(
                usage.Context,
                usage.Period,
                normalizedFilters,
                usage,
                summaries
            );
            await WriteAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    internal async Task SaveDailySummariesAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyCollection<CopilotDailyUsageSummary> summaries,
        IReadOnlyDictionary<string, string>? filters,
        CancellationToken cancellationToken)
    {

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var (document, key, normalizedFilters) = PrepareUpdate(context, period, filters);
            var existingUsage = GetExistingUsage(document, key);
            var merged = MergeDailySummaries(GetExistingDailySummaries(document, key), summaries);

            document.Entries[key] = CreateEntry(
                context,
                period,
                normalizedFilters,
                existingUsage,
                merged
            );
            await WriteAsync(document, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task WriteAsync(
        CopilotBillingCacheDocument document,
        CancellationToken cancellationToken)
        => await AtomicJsonFile.WriteAsync(
            _directoryPath,
            _billingPath,
            document,
            SERIALIZER_OPTIONS,
            cancellationToken
        ).ConfigureAwait(false);

    private static IReadOnlyList<CopilotDailyUsageSummary> GetExistingDailySummaries(
        CopilotBillingCacheDocument document,
        string key)
        => document.Entries.TryGetValue(key, out var existing)
            ? existing.DailySummaries
            : [];

    private static CopilotCreditUsage? GetExistingUsage(
        CopilotBillingCacheDocument document,
        string key)
        => document.Entries.TryGetValue(key, out var existing)
            ? existing.Usage
            : null;

    private CopilotBillingCacheEntry? FindMatchingEntry(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        string normalizedFilters)
    {

        var document = ReadDocument();

        if (document is null)
            return null;

        var key = BuildCacheKey(principalId, scope, ownerId, year, month, normalizedFilters);

        if (!document.Entries.TryGetValue(key, out var entry))
            return null;

        return MatchesContext(entry, principalId, scope, ownerId, year, month, normalizedFilters)
            ? entry
            : null;
    }

    private (CopilotBillingCacheDocument Document, string Key, string NormalizedFilters) PrepareUpdate(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        IReadOnlyDictionary<string, string>? filters)
    {

        var document = ReadDocument() ?? new CopilotBillingCacheDocument();
        var normalizedFilters = NormalizeFilters(filters);
        var key = BuildCacheKey(
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth,
            normalizedFilters
        );

        PruneOutdatedPeriods(
            document,
            context.PrincipalId,
            context.Scope,
            context.OwnerId,
            period.RequestedYear,
            period.RequestedMonth
        );

        return (document, key, normalizedFilters);
    }

    private static IReadOnlyList<CopilotDailyUsageSummary> MergeDailySummaries(
        IReadOnlyList<CopilotDailyUsageSummary> existing,
        IReadOnlyCollection<CopilotDailyUsageSummary> incoming)
    {

        var map = existing.ToDictionary(static summary => summary.Day);

        foreach (var summary in incoming)
            map[summary.Day] = summary;

        return map.Values.OrderBy(static summary => summary.Day).ToList();
    }

    private static CopilotBillingCacheEntry CreateEntry(
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
