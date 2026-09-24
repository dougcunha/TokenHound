using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Claude;

internal static class ClaudeQuotaWindowMapper
{
    private sealed record QuotaCandidate(string Name, double Percent, DateTimeOffset? Reset, string? Group);

    internal static IReadOnlyList<LimitWindow> Map(ClaudeUsageResponse? usage)
    {

        if (usage is null)
            return [];

        var windows = new Dictionary<string, LimitWindow>(StringComparer.OrdinalIgnoreCase);
        AddReportedLimits(windows, usage.Limits);
        AddBaseWindow(windows, ClaudeOAuthProvider.FIVE_HOUR_WINDOW_NAME, usage.FiveHour);
        AddBaseWindow(windows, ClaudeOAuthProvider.SEVEN_DAY_WINDOW_NAME, usage.SevenDay);
        AddTopLevelWindows(windows, usage.AdditionalFields);

        return windows.Values
            .OrderBy(static window => DisplayRank(window.Name))
            .ThenBy(static window => window.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static window => window.GroupName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddReportedLimits(Dictionary<string, LimitWindow> windows, JsonElement? limits)
    {

        if (limits?.ValueKind != JsonValueKind.Array)
            return;

        foreach (var entry in limits.Value.EnumerateArray())
        {

            var candidate = ParseLimit(entry);

            if (candidate is not null)
                AddIfMissing(windows, candidate);
        }
    }

    private static QuotaCandidate? ParseLimit(JsonElement entry)
    {

        if (entry.ValueKind != JsonValueKind.Object)
            return null;

        var kind = ReadString(entry, "kind");
        var percent = ReadPercent(entry, "percent");

        if (kind is null || percent is null || IsNonQuotaKind(kind))
            return null;

        var name = CanonicalName(kind);
        return new QuotaCandidate(
            name,
            percent.Value,
            ReadReset(entry),
            ReadScopeName(entry)
        );
    }

    /// <summary>
    /// Reads the display name of the model or surface a scoped limit applies to.
    /// The API reports <c>scope</c> as <c>{"model":{"display_name":...},"surface":...}</c>;
    /// <c>group</c> is only the limit category (for example <c>weekly</c>) and is never a scope.
    /// </summary>
    private static string? ReadScopeName(JsonElement entry)
    {

        if (!entry.TryGetProperty("scope", out var scope))
            return null;

        if (scope.ValueKind == JsonValueKind.String)
            return ReadString(entry, "scope");

        if (scope.ValueKind != JsonValueKind.Object)
            return null;

        return ReadDisplayName(scope, "model") ?? ReadDisplayName(scope, "surface");
    }

    private static string? ReadDisplayName(JsonElement scope, string name)
    {

        if (!scope.TryGetProperty(name, out var target))
            return null;

        return target.ValueKind switch
        {
            JsonValueKind.String => ReadString(scope, name),
            JsonValueKind.Object => ReadString(target, "display_name") ?? ReadString(target, "id"),
            _ => null
        };
    }

    private static void AddBaseWindow(
        Dictionary<string, LimitWindow> windows,
        string name,
        ClaudeWindowDto? detail)
    {

        if (detail?.Utilization is not double percent)
            return;

        AddIfMissing(windows, new QuotaCandidate(
            name,
            percent,
            detail.ResetsAt,
            null
        ));
    }

    private static void AddTopLevelWindows(
        Dictionary<string, LimitWindow> windows,
        Dictionary<string, JsonElement>? fields)
    {

        if (fields is null)
            return;

        foreach (var (key, value) in fields)
        {

            if (!key.StartsWith("seven_day_", StringComparison.OrdinalIgnoreCase)
                || key.Length == "seven_day_".Length
                || value.ValueKind != JsonValueKind.Object)
                continue;

            var percent = ReadPercent(value, "utilization");

            if (percent is null)
                continue;

            AddIfMissing(windows, new QuotaCandidate(
                key.ToLowerInvariant(),
                percent.Value,
                ReadReset(value),
                null
            ));
        }
    }

    private static void AddIfMissing(Dictionary<string, LimitWindow> windows, QuotaCandidate candidate)
    {

        var scoped = candidate.Name.EndsWith("_scoped", StringComparison.OrdinalIgnoreCase);
        var identity = scoped ? $"{candidate.Name}\u001f{candidate.Group}" : candidate.Name;

        if (windows.ContainsKey(identity))
            return;

        windows.Add(identity, new LimitWindow
        {
            Name = candidate.Name,
            GroupName = candidate.Group,
            UsedFraction = candidate.Percent / 100.0,
            ResetTimeUtc = candidate.Reset,
            Period = ResolvePeriod(candidate.Name)
        });
    }

    private static string CanonicalName(string kind)
    {

        if (string.Equals(kind, "session", StringComparison.OrdinalIgnoreCase))
            return ClaudeOAuthProvider.FIVE_HOUR_WINDOW_NAME;

        if (string.Equals(kind, "weekly_all", StringComparison.OrdinalIgnoreCase))
            return ClaudeOAuthProvider.SEVEN_DAY_WINDOW_NAME;

        if (kind.StartsWith("weekly_", StringComparison.OrdinalIgnoreCase)
            && !kind.EndsWith("_scoped", StringComparison.OrdinalIgnoreCase))
            return $"seven_day_{kind["weekly_".Length..]}".ToLowerInvariant();

        return kind.ToLowerInvariant();
    }

    private static bool IsNonQuotaKind(string kind)
        => kind.Contains("extra_usage", StringComparison.OrdinalIgnoreCase)
            || kind.Contains("spend", StringComparison.OrdinalIgnoreCase);

    private static string? ReadString(JsonElement entry, string name)
    {

        if (!entry.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String)
            return null;

        var value = property.GetString()?.Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static double? ReadPercent(JsonElement entry, string name)
        => entry.TryGetProperty(name, out var property) ? ToPercent(property) : null;

    private static DateTimeOffset? ReadReset(JsonElement entry)
        => entry.TryGetProperty("resets_at", out var property) ? ToReset(property) : null;

    /// <summary>
    /// Converts a JSON number to a finite percentage in [0, 100], or null when absent or invalid.
    /// </summary>
    internal static double? ToPercent(JsonElement value)
    {

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out var percent))
            return null;

        return double.IsFinite(percent) && percent is >= 0 and <= 100 ? percent : null;
    }

    /// <summary>
    /// Converts a JSON timestamp string to UTC, or null when absent or unreadable.
    /// </summary>
    internal static DateTimeOffset? ToReset(JsonElement value)
    {

        if (value.ValueKind != JsonValueKind.String)
            return null;

        return DateTimeOffset.TryParse(
            value.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var reset
        ) ? reset : null;
    }

    private static TimeSpan? ResolvePeriod(string name)
    {

        if (string.Equals(name, ClaudeOAuthProvider.FIVE_HOUR_WINDOW_NAME, StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromHours(5);

        return name.StartsWith("seven_day", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("weekly_", StringComparison.OrdinalIgnoreCase)
                ? TimeSpan.FromDays(7)
                : null;
    }

    private static int DisplayRank(string name)
    {

        if (string.Equals(name, ClaudeOAuthProvider.FIVE_HOUR_WINDOW_NAME, StringComparison.OrdinalIgnoreCase))
            return 0;

        return string.Equals(name, ClaudeOAuthProvider.SEVEN_DAY_WINDOW_NAME, StringComparison.OrdinalIgnoreCase)
            ? 1
            : 2;
    }
}
