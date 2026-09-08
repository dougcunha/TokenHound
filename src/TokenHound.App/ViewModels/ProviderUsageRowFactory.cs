using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Creates immutable <see cref="ProviderUsageRow"/> projections from domain telemetry snapshots.
/// </summary>
public static partial class ProviderUsageRowFactory
{
    private const string COPILOT_PROVIDER_ID = "copilot";

    /// <summary>
    /// Creates a read-only list of usage rows from a telemetry snapshot.
    /// </summary>
    /// <param name="snapshot">The domain telemetry snapshot.</param>
    /// <param name="timeProvider">Optional time provider for countdown calculations.</param>
    /// <returns>A read-only collection of presentation usage rows.</returns>
    public static IReadOnlyList<ProviderUsageRow> CreateRows(
        Snapshot snapshot,
        TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(snapshot);

        var provider = timeProvider ?? TimeProvider.System;
        var nowUtc = provider.GetUtcNow();
        var rows = new List<ProviderUsageRow>();

        AddQuotaRows(rows, snapshot, nowUtc);
        AddCopilotCreditRow(rows, snapshot, nowUtc);

        return rows;
    }

    private static void AddQuotaRows(
        List<ProviderUsageRow> rows,
        Snapshot snapshot,
        DateTimeOffset nowUtc)
    {

        for (var index = 0; index < snapshot.LimitWindows.Count; index++)
        {
            var window = snapshot.LimitWindows[index];
            var label = ResolveQuotaWindowLabel(snapshot.ProviderId, window, index);
            var resetTime = window.ResetTimeUtc ?? (index == 0 ? snapshot.ActiveBlock?.ResetTimeUtc : null);

            rows.Add(new ProviderUsageRow
            {
                Key = $"quota:{index}",
                Label = label,
                UsedFraction = window.UsedFraction,
                PrimaryQuantityText = ResolveQuotaPrimaryText(snapshot, window),
                SecondaryQuantityText = ResolveQuotaSecondaryText(snapshot, window),
                ResetText = FormatResetCountdown(resetTime, nowUtc)
            });
        }
    }

    private static string ResolveQuotaPrimaryText(Snapshot snapshot, LimitWindow window)
    {

        if (snapshot.Fidelity == Fidelity.Derived && window.UsedFraction is null && window.RemainingUnits is { } reqCount)
            return $"~{reqCount} requests";

        if (window.UsedFraction.HasValue)
            return $"{(int)Math.Round(window.UsedFraction.Value * 100.0)}% Used";

        if (window.RemainingUnits.HasValue)
            return $"~{window.RemainingUnits.Value} requests";

        return "Unmeasured";
    }

    private static string? ResolveQuotaSecondaryText(Snapshot snapshot, LimitWindow window)
    {

        if (window.RemainingUnits.HasValue && window.TotalUnits.HasValue)
            return $"{window.RemainingUnits.Value.ToString("N0", CultureInfo.InvariantCulture)} of {window.TotalUnits.Value.ToString("N0", CultureInfo.InvariantCulture)} remaining";

        if (snapshot.Fidelity == Fidelity.Derived && window.TotalUnits is null)
            return "No limit published";

        return null;
    }

    private static string ResolveQuotaWindowLabel(
        string providerId,
        LimitWindow window,
        int index)
    {

        if (string.Equals(providerId, COPILOT_PROVIDER_ID, StringComparison.OrdinalIgnoreCase))
        {
            if (window.Name.Contains("Premium", StringComparison.OrdinalIgnoreCase))
                return "Premium interactions";

            return !string.IsNullOrWhiteSpace(window.Name) ? window.Name : "Quota";
        }

        if (!string.IsNullOrWhiteSpace(window.Name))
        {
            if (window.Period?.TotalHours == 5 ||
                window.Name.Contains("session", StringComparison.OrdinalIgnoreCase) ||
                window.Name.Contains("five", StringComparison.OrdinalIgnoreCase))
                return "Current session (5h)";

            if (window.Period?.TotalDays == 7 ||
                window.Name.Contains("week", StringComparison.OrdinalIgnoreCase) ||
                window.Name.Contains("seven", StringComparison.OrdinalIgnoreCase))
                return "Weekly limit (7d)";

            return window.Name;
        }

        if (window.Period is { } period)
        {
            if (period.TotalHours <= 24)
                return $"Session ({(int)period.TotalHours}h)";

            return $"Period ({(int)period.TotalDays}d)";
        }

        return index == 0 ? "Current session (5h)" : "Weekly limit (7d)";
    }

    /// <summary>
    /// Formats a reset timestamp as a human-readable countdown string.
    /// </summary>
    /// <param name="resetTimeUtc">The reset timestamp, or null.</param>
    /// <param name="nowUtc">The reference current time.</param>
    /// <returns>A formatted countdown string, or null if unmeasured.</returns>
    public static string? FormatResetCountdown(
        DateTimeOffset? resetTimeUtc,
        DateTimeOffset nowUtc)
    {

        if (!resetTimeUtc.HasValue)
            return null;

        if (resetTimeUtc.Value <= nowUtc)
            return "Resets now";

        var diff = resetTimeUtc.Value - nowUtc;

        if (diff.TotalDays >= 1.0)
            return $"Resets in {(int)diff.TotalDays}d {diff.Hours}h";

        if (diff.TotalHours >= 1.0)
            return $"Resets in {(int)diff.TotalHours}h {diff.Minutes}m";

        return $"Resets in {Math.Max(1, (int)diff.TotalMinutes)}m";
    }
}
