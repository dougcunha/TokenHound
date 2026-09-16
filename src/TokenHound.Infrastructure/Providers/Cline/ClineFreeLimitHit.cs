using System;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents a locally recorded Cline free model limit failure.
/// </summary>
public sealed record ClineFreeLimitHit
{
    /// <summary>
    /// Gets the UTC timestamp when the CLI recorded the failure.
    /// </summary>
    public required DateTimeOffset DetectedAtUtc { get; init; }

    /// <summary>
    /// Gets the retry delay published inside the failure message, when it was readable.
    /// </summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Gets the recorded failure text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the UTC instant when the free model tier is expected to accept traffic again.
    /// </summary>
    public DateTimeOffset? ResetTimeUtc
        => RetryAfter is { } retryAfter ? DetectedAtUtc + retryAfter : null;

    /// <summary>
    /// Reports whether the decoded delay still applies at the supplied instant.
    /// </summary>
    /// <param name="nowUtc">The reference current time.</param>
    /// <returns><see langword="true"/> when a decoded reset instant is still in the future.</returns>
    public bool IsActive(DateTimeOffset nowUtc)
        => ResetTimeUtc is { } resetTimeUtc && resetTimeUtc > nowUtc;
}