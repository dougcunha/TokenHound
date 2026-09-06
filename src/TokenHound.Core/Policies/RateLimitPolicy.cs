using System;

namespace TokenHound.Core.Policies;

/// <summary>
/// Evaluates rate-limit deadlines and calculates backoff penalties honoring minimum floor constraints.
/// </summary>
public static class RateLimitPolicy
{
    /// <summary>
    /// Minimum rate limit penalty floor (60 seconds), protecting against infinite loops on <c>Retry-After: 0</c>.
    /// </summary>
    public static readonly TimeSpan MINIMUM_RETRY_FLOOR = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Determines whether a network request may be dispatched based on the recorded rate-limit deadline.
    /// </summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="deadlineUtc">The recorded deadline UTC timestamp, or null if no rate limit is active.</param>
    /// <returns><see langword="false"/> if <paramref name="nowUtc"/> is earlier than <paramref name="deadlineUtc"/>; otherwise, <see langword="true"/>.</returns>
    public static bool CanDispatch(DateTimeOffset nowUtc, DateTimeOffset? deadlineUtc)
    {

        if (deadlineUtc is null)
            return true;

        return !(nowUtc < deadlineUtc.Value);
    }

    /// <summary>
    /// Calculates the rate-limit deadline. If <paramref name="retryAfterSeconds"/> is provided, enforces a 60-second floor.
    /// If <paramref name="retryAfterSeconds"/> is null, calculates backoff using <see cref="BackoffCalculator"/> with the specified jitter factor.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="retryAfterSeconds">The server-reported Retry-After seconds, if any.</param>
    /// <param name="consecutiveFailures">The count of consecutive failures encountered.</param>
    /// <param name="jitterFactor">A factor between 0.0 and 1.0 for deterministic jitter testing when retryAfterSeconds is null.</param>
    /// <returns>The calculated deadline <see cref="DateTimeOffset"/>.</returns>
    public static DateTimeOffset CalculateDeadline(
        DateTimeOffset nowUtc,
        int? retryAfterSeconds,
        int consecutiveFailures,
        double jitterFactor)
    {

        if (retryAfterSeconds.HasValue)
        {
            var seconds = Math.Max((int)MINIMUM_RETRY_FLOOR.TotalSeconds, retryAfterSeconds.Value);

            return nowUtc.AddSeconds(seconds);
        }

        var backoff = BackoffCalculator.CalculateBackoff(consecutiveFailures, jitterFactor);

        return nowUtc + backoff;
    }

    /// <summary>
    /// Calculates the rate-limit deadline. If <paramref name="retryAfterSeconds"/> is provided, enforces a 60-second floor.
    /// If <paramref name="retryAfterSeconds"/> is null, calculates backoff using <see cref="BackoffCalculator"/>.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="retryAfterSeconds">The server-reported Retry-After seconds, if any.</param>
    /// <param name="consecutiveFailures">The count of consecutive failures encountered.</param>
    /// <param name="random">An optional <see cref="Random"/> instance for jitter generation when retryAfterSeconds is null.</param>
    /// <returns>The calculated deadline <see cref="DateTimeOffset"/>.</returns>
    public static DateTimeOffset CalculateDeadline(
        DateTimeOffset nowUtc,
        int? retryAfterSeconds,
        int consecutiveFailures,
        Random? random = null)
    {

        if (retryAfterSeconds.HasValue)
        {
            var seconds = Math.Max((int)MINIMUM_RETRY_FLOOR.TotalSeconds, retryAfterSeconds.Value);

            return nowUtc.AddSeconds(seconds);
        }

        var backoff = BackoffCalculator.CalculateBackoff(consecutiveFailures, random);

        return nowUtc + backoff;
    }
}
