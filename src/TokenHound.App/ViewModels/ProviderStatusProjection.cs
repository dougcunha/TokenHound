using System;
using System.Collections.Generic;
using System.Linq;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Projects provider snapshots into the grouped account rows of the provider status window.
/// </summary>
internal static class ProviderStatusProjection
{
    private const double EXHAUSTED_FRACTION = 1.0;
    private const string UNMEASURED_TEXT = "Unmeasured";
    private const string WAITING_TEXT = "Waiting for first reading";

    /// <summary>Creates the account row of a provider from its latest snapshot.</summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="snapshot">The latest snapshot, or null when none has been produced yet.</param>
    /// <param name="formatter">The formatter that owns the clock and culture.</param>
    /// <returns>The account row, pending when no snapshot exists.</returns>
    public static ProviderStatusAccount CreateAccount(
        string providerId,
        Snapshot? snapshot,
        ProviderStatusFormatter formatter)
    {

        var displayName = ProviderCatalog.ResolveDefaultName(providerId);

        if (snapshot is null)
            return CreatePendingAccount(providerId, displayName);

        var columns = ProviderUsageRowFactory.CreateRows(snapshot, formatter.TimeProvider)
            .Select(row => CreateColumn(row, formatter))
            .ToList();

        var exhausted = columns.Where(static column => column.IsExhausted).ToList();

        return new ProviderStatusAccount
        {
            ProviderId = providerId,
            DisplayName = displayName,
            StatusMessage = ProviderRingViewModel.ResolveStatusMessage(snapshot),
            IsBlocked = ResolveBlocked(snapshot),
            IsExhausted = exhausted.Count > 0,
            BackInText = exhausted.Count > 0 ? formatter.FormatBackIn(exhausted.Min(static c => c.ResetTimeUtc)) : null,
            Columns = columns
        };
    }

    /// <summary>Groups account rows by provider family, ordered by family name, default profile first.</summary>
    /// <param name="accounts">The account rows to group.</param>
    /// <returns>The ordered groups.</returns>
    public static IReadOnlyList<ProviderStatusGroup> Group(IEnumerable<ProviderStatusAccount> accounts)
        => [.. accounts
            .GroupBy(static account => ProviderCatalog.ResolveFamilyName(account.ProviderId), StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new ProviderStatusGroup
            {
                FamilyName = group.Key,
                Accounts =
                [
                    .. group
                        .OrderBy(static account => ProviderCatalog.IsDefaultClaudeProfile(account.ProviderId) ? 0 : 1)
                        .ThenBy(static account => account.DisplayName, StringComparer.OrdinalIgnoreCase)
                ]
            })];

    private static bool ResolveBlocked(Snapshot snapshot)
        => snapshot.Status is ProviderStatus.RateLimited or ProviderStatus.AccessDenied
            || snapshot.ActiveBlock?.IsBlocked == true;

    private static ProviderStatusAccount CreatePendingAccount(string providerId, string displayName)
        => new()
        {
            ProviderId = providerId,
            DisplayName = displayName,
            StatusMessage = WAITING_TEXT,
            IsPending = true
        };

    private static ProviderStatusColumn CreateColumn(ProviderUsageRow row, ProviderStatusFormatter formatter)
        => new()
        {
            Key = row.Key,
            Label = row.Label,
            ValueText = row.UsedFraction is { } fraction
                ? ProviderStatusFormatter.FormatPercent(fraction)
                : row.PrimaryQuantityText ?? UNMEASURED_TEXT,
            UsedFraction = row.UsedFraction,
            Level = ProviderStatusFormatter.ResolveLevel(row.UsedFraction),
            ResetText = formatter.FormatResetLine(row.ResetTimeUtc),
            ResetTimeUtc = row.ResetTimeUtc,
            IsExhausted = row.UsedFraction >= EXHAUSTED_FRACTION
        };
}
