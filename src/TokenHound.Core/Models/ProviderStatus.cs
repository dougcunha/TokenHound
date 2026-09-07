namespace TokenHound.Core.Models;

/// <summary>
/// Represents the health and operational availability status of a provider adapter.
/// </summary>
public enum ProviderStatus
{
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
    /// The provider denied access due to insufficient scope or invalid permissions.
    /// </summary>
    AccessDenied,

    /// <summary>
    /// The provider is actively rate-limited and awaiting deadline reset.
    /// </summary>
    RateLimited,

    /// <summary>
    /// The provider is reachable but has no usable entitlement or finite quota.
    /// </summary>
    Unsupported
}
