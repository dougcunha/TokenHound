using System;
using System.Collections.Generic;
using System.Globalization;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

public static partial class ProviderUsageRowFactory
{
    private const string CLINE_PROVIDER_ID = "cline";

    private static void AddClineRows(
        List<ProviderUsageRow> rows,
        Snapshot snapshot,
        DateTimeOffset nowUtc)
    {

        if (!string.Equals(snapshot.ProviderId, CLINE_PROVIDER_ID, StringComparison.OrdinalIgnoreCase))
            return;

        if (snapshot.ClineAccount is { } account)
            rows.Add(CreateClineCreditRow(account));

        if (snapshot.ClineLocal is { } local)
            rows.Add(CreateClineLocalRow(local));

        if (snapshot.ActiveBlock?.Reason is { } reason &&
            string.Equals(reason, "FreeModelLimitReached", StringComparison.OrdinalIgnoreCase))
            rows.Add(CreateClineFreeLimitRow(snapshot, nowUtc));
    }

    private static ProviderUsageRow CreateClineCreditRow(ClineAccountUsage account)
        => new()
        {
            Key = "cline:credits",
            Label = "Cline credits",
            UsedFraction = null,
            PrimaryQuantityText = account.BalanceCredits.HasValue
                ? $"{FormatDecimal(account.BalanceCredits.Value)} remaining"
                : "Unmeasured",
            SecondaryQuantityText = account.HasPassSubscription ? null : "No limit published",
            ScopeText = account.PlanName
        };

    private static ProviderUsageRow CreateClineLocalRow(ClineLocalUsage local)
        => new()
        {
            Key = "cline:local",
            Label = "Local tokens (24h)",
            UsedFraction = null,
            PrimaryQuantityText = $"{local.TotalTokens.ToString("N0", CultureInfo.InvariantCulture)} tokens",
            SecondaryQuantityText = $"{local.ModelCalls} model calls"
        };

    private static ProviderUsageRow CreateClineFreeLimitRow(Snapshot snapshot, DateTimeOffset nowUtc)
        => new()
        {
            Key = "cline:freelimit",
            Label = "Free model limit",
            UsedFraction = null,
            PrimaryQuantityText = "Limit reached",
            ResetText = FormatResetCountdown(snapshot.ActiveBlock?.ResetTimeUtc, nowUtc)
        };

    private static string FormatDecimal(double value)
        => value.ToString("#,##0.####", CultureInfo.InvariantCulture);
}