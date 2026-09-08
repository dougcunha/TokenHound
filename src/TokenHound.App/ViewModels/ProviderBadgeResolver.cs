using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Resolves the monitoring flag and the latest snapshot of a provider into a badge state,
/// its visible text label, and the resource keys of its pill brushes. Returns resource key
/// names only, so the resolver stays free of presentation framework types.
/// </summary>
public static class ProviderBadgeResolver
{
    private const string LABEL_CHECKING = "Checking...";
    private const string LABEL_OK = "OK";
    private const string LABEL_STALE = "Stale";
    private const string LABEL_NEEDS_AUTH = "Needs Auth";
    private const string LABEL_RATE_LIMITED = "Rate Limited";
    private const string LABEL_ACCESS_DENIED = "Access Denied";
    private const string LABEL_UNSUPPORTED = "No Quota";
    private const string LABEL_DISABLED = "Disabled";

    private const string BACKGROUND_CHECKING = "BadgeCheckingBackgroundBrush";
    private const string BACKGROUND_OK = "BadgeOkBackgroundBrush";
    private const string BACKGROUND_STALE = "BadgeStaleBackgroundBrush";
    private const string BACKGROUND_NEEDS_AUTH = "BadgeNeedsAuthBackgroundBrush";
    private const string BACKGROUND_RATE_LIMITED = "BadgeRateLimitedBackgroundBrush";
    private const string BACKGROUND_ACCESS_DENIED = "BadgeAccessDeniedBackgroundBrush";
    private const string BACKGROUND_DISABLED = "BadgeDisabledBackgroundBrush";

    private const string FOREGROUND_CHECKING = "BadgeCheckingForegroundBrush";
    private const string FOREGROUND_OK = "BadgeOkForegroundBrush";
    private const string FOREGROUND_STALE = "BadgeStaleForegroundBrush";
    private const string FOREGROUND_NEEDS_AUTH = "BadgeNeedsAuthForegroundBrush";
    private const string FOREGROUND_RATE_LIMITED = "BadgeRateLimitedForegroundBrush";
    private const string FOREGROUND_ACCESS_DENIED = "BadgeAccessDeniedForegroundBrush";
    private const string FOREGROUND_DISABLED = "BadgeDisabledForegroundBrush";

    /// <summary>Resolves the badge state for a provider row.</summary>
    /// <param name="isMonitored">Whether the user keeps monitoring enabled for the provider.</param>
    /// <param name="snapshot">The latest snapshot of the provider, or <see langword="null"/> when none arrived yet.</param>
    /// <returns>
    /// <see cref="ProviderBadgeState.Disabled"/> when monitoring is off, <see cref="ProviderBadgeState.Checking"/>
    /// when no snapshot exists yet, otherwise the state matching the reported provider health.
    /// </returns>
    public static ProviderBadgeState ResolveState(bool isMonitored, Snapshot? snapshot)
    {

        if (!isMonitored)
            return ProviderBadgeState.Disabled;

        if (snapshot is null)
            return ProviderBadgeState.Checking;

        return snapshot.Status switch
        {
            ProviderStatus.Ok => ProviderBadgeState.Ok,
            ProviderStatus.Stale => ProviderBadgeState.Stale,
            ProviderStatus.NeedsAuth => ProviderBadgeState.NeedsAuth,
            ProviderStatus.RateLimited => ProviderBadgeState.RateLimited,
            ProviderStatus.AccessDenied => ProviderBadgeState.AccessDenied,
            ProviderStatus.Unsupported => ProviderBadgeState.Unsupported,
            _ => ProviderBadgeState.Checking
        };
    }

    /// <summary>Resolves the visible text label of a badge state, so the state never relies on color alone.</summary>
    /// <param name="state">The badge state to describe.</param>
    /// <returns>A non-empty label for the state.</returns>
    public static string ResolveLabel(ProviderBadgeState state)
        => state switch
        {
            ProviderBadgeState.Ok => LABEL_OK,
            ProviderBadgeState.Stale => LABEL_STALE,
            ProviderBadgeState.NeedsAuth => LABEL_NEEDS_AUTH,
            ProviderBadgeState.RateLimited => LABEL_RATE_LIMITED,
            ProviderBadgeState.AccessDenied => LABEL_ACCESS_DENIED,
            ProviderBadgeState.Unsupported => LABEL_UNSUPPORTED,
            ProviderBadgeState.Disabled => LABEL_DISABLED,
            _ => LABEL_CHECKING
        };

    /// <summary>Resolves the resource key of the pill background brush for a badge state.</summary>
    /// <param name="state">The badge state to style.</param>
    /// <returns>The resource key name of the background brush.</returns>
    /// <remarks><see cref="ProviderBadgeState.Unsupported"/> shares the neutral slate treatment of
    /// <see cref="ProviderBadgeState.Stale"/>, which the product palette does not distinguish.</remarks>
    public static string ResolveBackgroundKey(ProviderBadgeState state)
        => state switch
        {
            ProviderBadgeState.Ok => BACKGROUND_OK,
            ProviderBadgeState.Stale => BACKGROUND_STALE,
            ProviderBadgeState.NeedsAuth => BACKGROUND_NEEDS_AUTH,
            ProviderBadgeState.RateLimited => BACKGROUND_RATE_LIMITED,
            ProviderBadgeState.AccessDenied => BACKGROUND_ACCESS_DENIED,
            ProviderBadgeState.Unsupported => BACKGROUND_STALE,
            ProviderBadgeState.Disabled => BACKGROUND_DISABLED,
            _ => BACKGROUND_CHECKING
        };

    /// <summary>Resolves the resource key of the pill foreground brush for a badge state.</summary>
    /// <param name="state">The badge state to style.</param>
    /// <returns>The resource key name of the foreground brush.</returns>
    /// <remarks><see cref="ProviderBadgeState.Unsupported"/> shares the neutral slate treatment of
    /// <see cref="ProviderBadgeState.Stale"/>, which the product palette does not distinguish.</remarks>
    public static string ResolveForegroundKey(ProviderBadgeState state)
        => state switch
        {
            ProviderBadgeState.Ok => FOREGROUND_OK,
            ProviderBadgeState.Stale => FOREGROUND_STALE,
            ProviderBadgeState.NeedsAuth => FOREGROUND_NEEDS_AUTH,
            ProviderBadgeState.RateLimited => FOREGROUND_RATE_LIMITED,
            ProviderBadgeState.AccessDenied => FOREGROUND_ACCESS_DENIED,
            ProviderBadgeState.Unsupported => FOREGROUND_STALE,
            ProviderBadgeState.Disabled => FOREGROUND_DISABLED,
            _ => FOREGROUND_CHECKING
        };
}
