using System;
using System.Collections.Generic;
using System.Globalization;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

public static partial class ProviderUsageRowFactory
{
    private static void AddCopilotCreditRow(
        List<ProviderUsageRow> rows,
        Snapshot snapshot,
        DateTimeOffset nowUtc)
    {

        if (!string.Equals(snapshot.ProviderId, COPILOT_PROVIDER_ID, StringComparison.OrdinalIgnoreCase))
            return;

        if (snapshot.CopilotBilling is not { } billing)
            return;

        if (billing.Usage is { } usage)
        {
            rows.Add(new ProviderUsageRow
            {
                Key = "copilot:credits",
                Label = "AI credits",
                UsedFraction = (double?)usage.UsedFraction,
                PrimaryQuantityText = FormatCreditPrimaryQuantity(usage),
                SecondaryQuantityText = FormatCreditSecondaryQuantity(usage),
                ScopeText = FormatScopeText(usage.Context),
                ResetText = FormatResetCountdown(usage.Period.ResetUtc, nowUtc),
                ProvenanceText = FormatProvenanceText(usage, billing.State),
                ErrorText = FormatBillingError(billing.Reason, billing.NextRequestAtUtc, nowUtc)
            });

            return;
        }

        rows.Add(new ProviderUsageRow
        {
            Key = "copilot:credits",
            Label = "AI credits",
            UsedFraction = null,
            PrimaryQuantityText = "Unavailable",
            ErrorText = FormatBillingError(billing.Reason, billing.NextRequestAtUtc, nowUtc) ?? "Billing unavailable"
        });
    }

    private static string FormatCreditPrimaryQuantity(CopilotCreditUsage usage)
    {

        if (usage.Remaining.HasValue)
            return $"{FormatDecimal(usage.Remaining.Value)} remaining";

        if (usage.GrossUsed.HasValue)
            return $"{FormatDecimal(usage.GrossUsed.Value)} credits used";

        if (usage.NetUsed.HasValue)
            return $"{FormatDecimal(usage.NetUsed.Value)} credits used";

        return "Unmeasured";
    }

    private static string? FormatCreditSecondaryQuantity(CopilotCreditUsage usage)
    {

        if (usage.IncludedTotal.HasValue)
            return $"{FormatDecimal(usage.GrossUsed ?? 0)} of {FormatDecimal(usage.IncludedTotal.Value)} total";

        if (usage.DiscountedUsed.HasValue && usage.DiscountedUsed.Value > 0)
            return $"Gross: {FormatDecimal(usage.GrossUsed ?? 0)} · Net: {FormatDecimal(usage.NetUsed ?? 0)}";

        if (usage.GrossUsed.HasValue && usage.NetUsed.HasValue && usage.GrossUsed.Value != usage.NetUsed.Value)
            return $"Gross: {FormatDecimal(usage.GrossUsed.Value)}";

        return null;
    }

    private static string? FormatScopeText(CopilotBillingContext context)
    {

        if (!string.IsNullOrWhiteSpace(context.OwnerName))
            return $"{context.OwnerName} ({context.Scope})";

        if (context.Scope != CopilotBillingScope.Unknown)
            return $"{context.Scope}";

        return null;
    }

    private static string? FormatProvenanceText(
        CopilotCreditUsage usage,
        CopilotBillingState state)
    {

        var parts = new List<string>();

        parts.Add(usage.Source == CopilotCreditSource.BillingApi ? "Direct billing" : "Daily report");

        if (usage.IsEstimated)
            parts.Add("Estimated");

        if (!usage.Coverage.IsComplete)
        {
            if (usage.Coverage.MissingDays.Count > 0)
            {
                parts.Add($"Partial ({usage.Coverage.MissingDays.Count}d missing)");
            }
            else
            {
                parts.Add("Partial coverage");
            }
        }

        if (usage.SourceAsOfUtc.HasValue)
            parts.Add($"As of {usage.SourceAsOfUtc.Value:yyyy-MM-dd}");

        if (state == CopilotBillingState.Stale)
            parts.Add("Stale");

        return string.Join(" · ", parts);
    }

    private static string? FormatBillingError(
        CopilotBillingReason reason,
        DateTimeOffset? nextRequestAtUtc,
        DateTimeOffset nowUtc)
    {

        if (reason == CopilotBillingReason.None)
            return null;

        if (reason == CopilotBillingReason.RateLimited)
        {
            if (nextRequestAtUtc.HasValue && nextRequestAtUtc.Value > nowUtc)
            {
                var diff = nextRequestAtUtc.Value - nowUtc;
                var seconds = Math.Max(1, (int)diff.TotalSeconds);

                return $"Rate limited: retrying after {seconds}s";
            }

            return "Rate limited";
        }

        return reason switch
        {
            CopilotBillingReason.MissingCredential => "GitHub credentials not found",
            CopilotBillingReason.UnknownScope => "Billing organization or account could not be resolved",
            CopilotBillingReason.AmbiguousScope => "Multiple candidate billing scopes found",
            CopilotBillingReason.AccessDenied => "Access denied to billing API (check permissions)",
            CopilotBillingReason.ReportUnavailable => "Daily usage reports unavailable",
            CopilotBillingReason.NetworkFailure => "Network error retrieving billing",
            CopilotBillingReason.InvalidData => "Invalid billing telemetry data received",
            CopilotBillingReason.PersistenceFailure => "Failed to persist billing state",
            _ => "Billing unavailable"
        };
    }

    private static string FormatDecimal(decimal value)
        => value.ToString("#,##0.####", CultureInfo.InvariantCulture);
}
