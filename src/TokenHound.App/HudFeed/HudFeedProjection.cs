using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.HudFeed;

namespace TokenHound.App.HudFeed;

/// <summary>Projects the HUD rings into the HUD feed document, with the same values and texts the HUD shows.</summary>
internal static class HudFeedProjection
{
    private const string MOCK_PROVIDER_ID = "mock";

    /// <summary>Creates the feed document from the rings that the HUD shows now.</summary>
    /// <param name="rings">The HUD rings, in HUD order.</param>
    /// <param name="writtenAtUtc">The write time.</param>
    /// <param name="appVersion">The TokenHound version.</param>
    /// <param name="heartbeat">The longest interval between two writes.</param>
    /// <returns>The document, without the synthetic mock provider.</returns>
    public static HudFeedDocument Create(
        IEnumerable<ProviderRingViewModel> rings,
        DateTimeOffset writtenAtUtc,
        string appVersion,
        TimeSpan heartbeat)
    {

        ArgumentNullException.ThrowIfNull(rings);

        return new HudFeedDocument
        {
            WrittenAtUtc = writtenAtUtc,
            IsRunning = true,
            AppVersion = appVersion,
            HeartbeatSeconds = (int)Math.Ceiling(heartbeat.TotalSeconds),
            Providers = [.. rings.Where(static ring => !IsSynthetic(ring.ProviderId)).Select(MapProvider)]
        };
    }

    private static HudFeedProvider MapProvider(ProviderRingViewModel ring)
        => new()
        {
            ProviderId = ring.ProviderId,
            DisplayName = ring.ProviderName,
            FamilyName = ProviderCatalog.ResolveFamilyName(ring.ProviderId),
            Badge = ring.ProviderBadge,
            Status = JsonNamingPolicy.CamelCase.ConvertName(ring.Status.ToString()),
            StatusMessage = ring.StatusMessage,
            IsBusy = ring.IsBusy,
            ActiveSessionText = ring.ActiveSessionText,
            SessionUsedFraction = ring.SessionUsedFraction,
            SessionResetText = ring.SessionResetText,
            WeeklyUsedFraction = ring.WeeklyUsedFraction,
            WeeklyResetText = ring.WeeklyResetText,
            Rows = [.. ring.Rows.Select(MapRow)]
        };

    private static HudFeedRow MapRow(ProviderUsageRow row)
        => new()
        {
            Key = row.Key,
            Label = row.Label,
            UsedFraction = row.UsedFraction,
            PrimaryText = row.PrimaryQuantityText,
            SecondaryText = row.SecondaryQuantityText,
            ScopeText = row.ScopeText,
            ResetText = row.ResetText,
            ResetTimeUtc = row.ResetTimeUtc,
            ProvenanceText = row.ProvenanceText,
            ErrorText = row.ErrorText
        };

    private static bool IsSynthetic(string providerId)
        => string.Equals(providerId, MOCK_PROVIDER_ID, StringComparison.OrdinalIgnoreCase);
}
