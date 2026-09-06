using System;

namespace TokenHound.Infrastructure.Providers.Codex;

/// <summary>
/// Represents the rate-limit response returned by the Codex app server.
/// </summary>
public sealed record CodexRateLimitsDto
{
    /// <summary>
    /// Gets the account subscription plan type, if supplied by the app server.
    /// </summary>
    public string? PlanType { get; init; }

    /// <summary>
    /// Gets the active rate-limit reason, if the account is currently limited.
    /// </summary>
    public string? RateLimitReachedType { get; init; }

    /// <summary>
    /// Gets the primary rolling rate-limit window.
    /// </summary>
    public RateLimitWindow? Primary { get; init; }

    /// <summary>
    /// Gets the secondary rolling rate-limit window.
    /// </summary>
    public RateLimitWindow? Secondary { get; init; }

    /// <summary>
    /// Represents one Codex rate-limit window.
    /// </summary>
    public sealed record RateLimitWindow
    {
        /// <summary>
        /// Gets the percentage of the window consumed, from 0 to 100.
        /// </summary>
        public double? UsedPercent { get; init; }

        /// <summary>
        /// Gets the window duration in minutes.
        /// </summary>
        public int? WindowDurationMins { get; init; }

        /// <summary>
        /// Gets the Unix timestamp in seconds when the window resets.
        /// </summary>
        public long? ResetsAt { get; init; }
    }
}
