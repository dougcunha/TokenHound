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
    /// Maximum rate-limit wait before positive jitter is applied (900 seconds).
    /// </summary>
    public static readonly TimeSpan MAXIMUM_RETRY_CEILING = TimeSpan.FromSeconds(900);

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
    /// Calculates the rate-limit deadline. A server value raises the calculated wait floor and never lowers it.
    /// Positive one-to-five-second jitter is added after the shared ceiling is applied.
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

        var backoff = retryAfterSeconds.HasValue
            ? CalculateWait(retryAfterSeconds, consecutiveFailures, jitterFactor)
            : BackoffCalculator.CalculateBackoff(consecutiveFailures, jitterFactor);

        return nowUtc + backoff;
    }

    /// <summary>
    /// Calculates the rate-limit deadline. A server value raises the calculated wait floor and never lowers it.
    /// Positive one-to-five-second jitter is added after the shared ceiling is applied.
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

        var backoff = retryAfterSeconds.HasValue
            ? CalculateWait(retryAfterSeconds, consecutiveFailures, random)
            : BackoffCalculator.CalculateBackoff(consecutiveFailures, random);

        return nowUtc + backoff;
    }

    private static TimeSpan CalculateWait(
        int? retryAfterSeconds,
        int consecutiveFailures,
        double jitterFactor)
    {

        var baseInterval = BackoffCalculator.CalculateExponentialInterval(Math.Max(1, consecutiveFailures));
        var serverInterval = TimeSpan.FromSeconds(Math.Max(0, retryAfterSeconds.GetValueOrDefault()));
        var wait = TimeSpan.FromSeconds(Math.Min(
            MAXIMUM_RETRY_CEILING.TotalSeconds,
            Math.Max(baseInterval.TotalSeconds, serverInterval.TotalSeconds)
        ));
        var jitter = BackoffCalculator.CalculateBackoff(1, jitterFactor) - MINIMUM_RETRY_FLOOR;

        return wait + jitter;
    }

    private static TimeSpan CalculateWait(
        int? retryAfterSeconds,
        int consecutiveFailures,
        Random? random)
    {

        var baseInterval = BackoffCalculator.CalculateExponentialInterval(Math.Max(1, consecutiveFailures));
        var serverInterval = TimeSpan.FromSeconds(Math.Max(0, retryAfterSeconds.GetValueOrDefault()));
        var wait = TimeSpan.FromSeconds(Math.Min(
            MAXIMUM_RETRY_CEILING.TotalSeconds,
            Math.Max(baseInterval.TotalSeconds, serverInterval.TotalSeconds)
        ));
        var jitter = BackoffCalculator.CalculateBackoff(1, random) - MINIMUM_RETRY_FLOOR;

        return wait + jitter;
    }
}
