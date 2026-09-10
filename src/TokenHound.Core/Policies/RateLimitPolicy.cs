using System;
using System.Threading;

namespace TokenHound.Core.Policies;

/// <summary>
/// Evaluates rate-limit deadlines and calculates backoff penalties honoring minimum floor constraints.
/// </summary>
public sealed class RateLimitPolicy
{
    /// <summary>
    /// Minimum rate limit penalty floor (60 seconds), protecting against infinite loops on <c>Retry-After: 0</c>.
    /// </summary>
    public static readonly TimeSpan MINIMUM_RETRY_FLOOR = TimeSpan.FromSeconds(60);

    private long _effectiveRetryFloorTicks;

    /// <summary>
    /// Initializes an isolated rate-limit policy with the requested retry floor.
    /// </summary>
    /// <param name="effectiveRetryFloor">The requested retry floor, or the safety minimum when omitted.</param>
    public RateLimitPolicy(TimeSpan? effectiveRetryFloor = null)
    {

        SetEffectiveFloor(effectiveRetryFloor ?? MINIMUM_RETRY_FLOOR);
    }

    /// <summary>
    /// Gets the currently configured effective retry floor, never less than <see cref="MINIMUM_RETRY_FLOOR"/>.
    /// </summary>
    public TimeSpan EffectiveRetryFloor
        => TimeSpan.FromTicks(Interlocked.Read(ref _effectiveRetryFloorTicks));

    /// <summary>
    /// Sets the effective retry floor, clamping values below <see cref="MINIMUM_RETRY_FLOOR"/> to <see cref="MINIMUM_RETRY_FLOOR"/>.
    /// </summary>
    /// <param name="floor">The requested retry floor duration.</param>
    public void SetEffectiveFloor(TimeSpan floor)
    {

        var safeFloor = floor < MINIMUM_RETRY_FLOOR ? MINIMUM_RETRY_FLOOR : floor;

        Interlocked.Exchange(ref _effectiveRetryFloorTicks, safeFloor.Ticks);
    }

    /// <summary>
    /// Resets the effective retry floor to the default <see cref="MINIMUM_RETRY_FLOOR"/>.
    /// </summary>
    public void ResetEffectiveFloor()
        => SetEffectiveFloor(MINIMUM_RETRY_FLOOR);

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
    /// Calculates the rate-limit deadline as the longest of the server-reported <paramref name="retryAfterSeconds"/>
    /// and the jittered exponential backoff for <paramref name="consecutiveFailures"/>. The jittered value is floored at the
    /// previous exponential tier (never below <see cref="EffectiveRetryFloor"/>), so jitter varies the wait without undoing the
    /// escalation, and a provider that keeps replying <c>Retry-After: 0</c> still backs off instead of being retried every minute.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="retryAfterSeconds">The server-reported Retry-After seconds, if any.</param>
    /// <param name="consecutiveFailures">The count of consecutive failures encountered.</param>
    /// <param name="jitterFactor">A factor between 0.0 and 1.0 for deterministic jitter testing.</param>
    /// <returns>The calculated deadline <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset CalculateDeadline(
        DateTimeOffset nowUtc,
        int? retryAfterSeconds,
        int consecutiveFailures,
        double jitterFactor)
    {

        var backoff = BackoffCalculator.CalculateBackoff(consecutiveFailures, jitterFactor);

        return nowUtc + ResolvePenalty(retryAfterSeconds, backoff, consecutiveFailures);
    }

    /// <summary>
    /// Calculates the rate-limit deadline as the longest of the server-reported <paramref name="retryAfterSeconds"/>
    /// and the jittered exponential backoff for <paramref name="consecutiveFailures"/>, floored at the previous exponential tier.
    /// </summary>
    /// <param name="nowUtc">The current UTC timestamp.</param>
    /// <param name="retryAfterSeconds">The server-reported Retry-After seconds, if any.</param>
    /// <param name="consecutiveFailures">The count of consecutive failures encountered.</param>
    /// <param name="random">An optional <see cref="Random"/> instance for jitter generation.</param>
    /// <returns>The calculated deadline <see cref="DateTimeOffset"/>.</returns>
    public DateTimeOffset CalculateDeadline(
        DateTimeOffset nowUtc,
        int? retryAfterSeconds,
        int consecutiveFailures,
        Random? random = null)
    {

        var backoff = BackoffCalculator.CalculateBackoff(consecutiveFailures, random);

        return nowUtc + ResolvePenalty(retryAfterSeconds, backoff, consecutiveFailures);
    }

    private TimeSpan ResolvePenalty(
        int? retryAfterSeconds,
        TimeSpan backoff,
        int consecutiveFailures)
    {

        var effectiveFloor = EffectiveRetryFloor;
        var flooredBackoff = ApplyMinimumFloor(backoff, consecutiveFailures, effectiveFloor);

        if (!retryAfterSeconds.HasValue)
            return flooredBackoff;

        var serverPenalty = TimeSpan.FromSeconds(Math.Max(effectiveFloor.TotalSeconds, retryAfterSeconds.Value));

        return serverPenalty > flooredBackoff ? serverPenalty : flooredBackoff;
    }

    private static TimeSpan ApplyMinimumFloor(
        TimeSpan backoff,
        int consecutiveFailures,
        TimeSpan effectiveFloor)
    {

        if (consecutiveFailures <= 0)
            return TimeSpan.Zero;

        var previousTier = BackoffCalculator.CalculateExponentialInterval(consecutiveFailures - 1);
        var floor = previousTier > effectiveFloor ? previousTier : effectiveFloor;

        return backoff < floor ? floor : backoff;
    }
}
