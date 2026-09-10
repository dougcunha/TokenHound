using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

internal sealed partial class CopilotBillingArchive
{
    private static string BuildCacheKey(
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
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => $"{pair.Key.Trim().ToLowerInvariant()}={pair.Value.Trim().ToLowerInvariant()}")
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
        CopilotBillingCacheDocument document,
        string principalId,
        CopilotBillingScope scope,
        string? ownerId,
        int currentYear,
        int currentMonth)
    {

        var precedingYear = currentMonth == 1 ? currentYear - 1 : currentYear;
        var precedingMonth = currentMonth == 1 ? 12 : currentMonth - 1;
        var keysToRemove = new List<string>();

        foreach (var (key, entry) in document.Entries)
        {
            if (!MatchesOwner(entry, principalId, scope, ownerId))
                continue;

            var isCurrent = entry.Year == currentYear && entry.Month == currentMonth;
            var isPreceding = entry.Year == precedingYear && entry.Month == precedingMonth;

            if (!isCurrent && !isPreceding)
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
            document.Entries.Remove(key);
    }

    private static bool MatchesOwner(
        CopilotBillingCacheEntry entry,
        string principalId,
        CopilotBillingScope scope,
        string? ownerId)
        => string.Equals(entry.PrincipalId, principalId, StringComparison.OrdinalIgnoreCase)
            && entry.Scope == scope
            && string.Equals(entry.OwnerId ?? string.Empty, ownerId ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private CopilotBillingCacheDocument? ReadDocument()
    {

        if (!File.Exists(_billingPath))
            return null;

        try
        {
            var json = File.ReadAllText(_billingPath);
            var document = JsonSerializer.Deserialize<CopilotBillingCacheDocument>(json, SERIALIZER_OPTIONS);

            return document?.Version == SCHEMA_VERSION ? document : null;
        }
        catch (Exception ex) when (IsPersistenceException(ex))
        {
            return null;
        }
    }

    private static bool IsPersistenceException(Exception exception)
        => exception is JsonException
            or NotSupportedException
            or IOException
            or UnauthorizedAccessException
            or InvalidDataException;

    private sealed record CopilotBillingCacheDocument
    {
        public int Version { get; init; } = SCHEMA_VERSION;
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
}
