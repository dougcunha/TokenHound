using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageArchive
{
    private sealed record CopilotBillingCacheDocument
    {
        public int Version { get; init; } = COPILOT_BILLING_SCHEMA_VERSION;
        public Dictionary<string, CopilotBillingCacheEntry> Entries { get; init; } = new(StringComparer.Ordinal);
    }

    private sealed record CopilotBillingCacheEntry
    {
        public required string PrincipalId { get; init; }
        public required CopilotBillingScope Scope { get; init; }
        public string? OwnerId { get; init; }
        public required int Year { get; init; }
        public required int Month { get; init; }
        public required string NormalizedFilters { get; init; }
        public CopilotCreditUsage? Usage { get; init; }
        public IReadOnlyList<CopilotDailyUsageSummary> DailySummaries { get; init; } = [];
        public required DateTimeOffset UpdatedAtUtc { get; init; }
    }

    private static string BuildBillingCacheKey(
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        string normalizedFilters)
        => $"{principalId.Trim().ToLowerInvariant()}|{scope}|{(ownerId ?? string.Empty).Trim().ToLowerInvariant()}|{year:D4}-{month:D2}|{normalizedFilters}";

    private static string NormalizeFilters(IReadOnlyDictionary<string, string>? filters)
    {

        if (filters is null || filters.Count == 0)
            return string.Empty;

        return string.Join(
            ";",
            filters
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .Select(kv => $"{kv.Key.Trim().ToLowerInvariant()}={kv.Value.Trim().ToLowerInvariant()}")
        );
    }

    private static bool MatchesContext(
        CopilotBillingCacheEntry entry,
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int year,
        int month,
        string normalizedFilters)
    {

        if (!string.Equals(entry.PrincipalId, principalId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (entry.Scope != scope)
            return false;

        if (!string.Equals(entry.OwnerId ?? string.Empty, ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            return false;

        if (entry.Year != year || entry.Month != month)
            return false;

        return string.Equals(entry.NormalizedFilters, normalizedFilters, StringComparison.OrdinalIgnoreCase);
    }

    private static void PruneOutdatedPeriods(
        CopilotBillingCacheDocument doc,
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int currentYear,
        int currentMonth)
    {

        var precedingYear = currentMonth == 1 ? currentYear - 1 : currentYear;
        var precedingMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var keysToRemove = new List<string>();

        foreach (var (key, entry) in doc.Entries)
        {

            if (!string.Equals(entry.PrincipalId, principalId, StringComparison.OrdinalIgnoreCase)
                || entry.Scope != scope
                || !string.Equals(entry.OwnerId ?? string.Empty, ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isCurrent = entry.Year == currentYear && entry.Month == currentMonth;
            var isPreceding = entry.Year == precedingYear && entry.Month == precedingMonth;

            if (!isCurrent && !isPreceding)
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
            doc.Entries.Remove(key);
    }

    private CopilotBillingCacheDocument? ReadBillingDocument()
    {

        if (!File.Exists(CopilotBillingPath))
            return null;

        try
        {

            var json = File.ReadAllText(CopilotBillingPath);
            var doc = JsonSerializer.Deserialize<CopilotBillingCacheDocument>(json, SERIALIZER_OPTIONS);

            return doc?.Version == COPILOT_BILLING_SCHEMA_VERSION ? doc : null;
        }
        catch (Exception ex) when (IsPersistenceException(ex))
        {

            return null;
        }
    }

    private CopilotBillingCacheDocument ReadBillingDocumentForWrite()
        => ReadBillingDocument() ?? new CopilotBillingCacheDocument();
}
