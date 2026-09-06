namespace TokenHound.Core.Models;

/// <summary>
/// Represents an active blocking condition or rate-limit throttle applied by a provider.
/// </summary>
public sealed record UsageBlock
{
    /// <summary>
    /// Gets the reason or diagnostic message explaining why the provider is blocked.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Gets a value indicating whether requests to the provider are currently blocked.
    /// </summary>
    public required bool IsBlocked { get; init; }

    /// <summary>
    /// Gets the timestamp when the block or rate-limit penalty expires, if known.
    /// </summary>
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>
    /// Gets the retry-after duration in seconds supplied by the provider, if available.
    /// </summary>
    public int? RetryAfterSeconds { get; init; }
}
