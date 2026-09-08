namespace TokenHound.App.ViewModels;

/// <summary>
/// Represents the presentation state of a provider status badge in the Settings dialog.
/// Extends the health states of <see cref="TokenHound.Core.Models.ProviderStatus"/> with the
/// user configuration state <see cref="Disabled"/> and the lifecycle state <see cref="Checking"/>.
/// </summary>
public enum ProviderBadgeState
{
    /// <summary>
    /// The provider is monitored but has not resolved its first snapshot yet.
    /// </summary>
    Checking,

    /// <summary>
    /// The provider is operational and reported data is within freshness thresholds.
    /// </summary>
    Ok,

    /// <summary>
    /// The provider reported data that has exceeded the freshness threshold.
    /// </summary>
    Stale,

    /// <summary>
    /// The provider requires user authentication or re-authentication.
    /// </summary>
    NeedsAuth,

    /// <summary>
    /// The provider is actively rate-limited and awaiting deadline reset.
    /// </summary>
    RateLimited,

    /// <summary>
    /// The provider denied access due to insufficient scope or invalid permissions.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The provider is reachable but has no usable entitlement or finite quota.
    /// </summary>
    Unsupported,

    /// <summary>
    /// The user turned monitoring off for the provider, so no telemetry is collected.
    /// </summary>
    Disabled
}
