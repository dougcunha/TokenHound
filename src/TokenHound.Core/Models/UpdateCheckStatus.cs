namespace TokenHound.Core.Models;

/// <summary>
/// The result category of an update check.
/// </summary>
public enum UpdateCheckStatus
{
    /// <summary>The running version is the latest stable release, or no newer stable release exists.</summary>
    UpToDate,

    /// <summary>A newer stable release is available.</summary>
    Available,

    /// <summary>A newer stable release exists but the user chose to skip that version.</summary>
    Skipped,

    /// <summary>The check was not sent because a persisted rate-limit deadline is still in force.</summary>
    RateLimited,

    /// <summary>The check cannot be evaluated, for example because a version is not parseable.</summary>
    Unavailable,

    /// <summary>The check failed, for example because the network or the service returned an error.</summary>
    Failed
}
