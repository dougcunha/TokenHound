using System;

namespace TokenHound.Core.Policies;

/// <summary>
/// Computes exponential backoff intervals with full jitter for rate-limit and fault resilience.
/// </summary>
public static class BackoffCalculator
{
    /// <summary>
    /// The minimum base interval floor (60 seconds).
    /// </summary>
    public static readonly TimeSpan MINIMUM_FLOOR = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The maximum ceiling interval cap (3600 seconds / 1 hour).
    /// </summary>
    public static readonly TimeSpan MAXIMUM_CEILING = TimeSpan.FromSeconds(3600);

    /// <summary>
    /// Gets the minimum base interval floor (60 seconds).
    /// </summary>
    public static TimeSpan Floor
        => MINIMUM_FLOOR;

    /// <summary>
    /// Gets the maximum ceiling interval cap (3600 seconds).
    /// </summary>
    public static TimeSpan Ceiling
        => MAXIMUM_CEILING;

    /// <summary>
    /// Calculates the deterministic exponential backoff interval before jitter is applied.
    /// Formula: <c>min(Ceiling, Floor * 2^(consecutiveFailures - 1))</c>.
    /// </summary>
    /// <param name="consecutiveFailures">The number of consecutive failures encountered.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the capped exponential interval, or <see cref="TimeSpan.Zero"/> if failures is zero or negative.</returns>
    public static TimeSpan CalculateExponentialInterval(int consecutiveFailures)
    {

        if (consecutiveFailures <= 0)
            return TimeSpan.Zero;

        var exponent = Math.Min(consecutiveFailures - 1, 10);
        var unjitteredSeconds = MINIMUM_FLOOR.TotalSeconds * Math.Pow(2, exponent);
        var cappedSeconds = Math.Min(MAXIMUM_CEILING.TotalSeconds, unjitteredSeconds);

        return TimeSpan.FromSeconds(cappedSeconds);
    }

    /// <summary>
    /// Calculates the expected (unjittered) interval for the given number of consecutive failures.
    /// </summary>
    /// <param name="consecutiveFailures">The number of consecutive failures encountered.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the expected interval.</returns>
    public static TimeSpan CalculateExpectedInterval(int consecutiveFailures)
        => CalculateExponentialInterval(consecutiveFailures);

    /// <summary>
    /// Computes exponential backoff with full jitter applied using a specified jitter factor between 0.0 and 1.0.
    /// </summary>
    /// <param name="consecutiveFailures">The number of consecutive failures encountered.</param>
    /// <param name="jitterFactor">A factor between 0.0 (inclusive) and 1.0 (inclusive) specifying the uniform jitter position.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the jittered backoff interval.</returns>
    public static TimeSpan CalculateBackoff(int consecutiveFailures, double jitterFactor)
    {

        if (consecutiveFailures <= 0)
            return TimeSpan.Zero;

        var maxInterval = CalculateExponentialInterval(consecutiveFailures);
        var clampedFactor = Math.Clamp(jitterFactor, 0.0, 1.0);

        return TimeSpan.FromSeconds(maxInterval.TotalSeconds * clampedFactor);
    }

    /// <summary>
    /// Computes exponential backoff with full jitter applied using an optional random number generator.
    /// </summary>
    /// <param name="consecutiveFailures">The number of consecutive failures encountered.</param>
    /// <param name="random">An optional <see cref="Random"/> instance. If null, <see cref="Random.Shared"/> is used.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the jittered backoff interval.</returns>
    public static TimeSpan CalculateBackoff(int consecutiveFailures, Random? random = null)
    {
        var rng = random ?? Random.Shared;

        return CalculateBackoff(consecutiveFailures, rng.NextDouble());
    }
}
