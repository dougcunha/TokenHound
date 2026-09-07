using Serilog;
using System;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Guards provider dispatch against recorded rate-limit deadlines and records the resulting transitions in the log.
/// </summary>
internal static class RateLimitGate
{
    /// <summary>
    /// Evaluates whether a recorded deadline currently blocks dispatch, logging the skipped refresh when it does.
    /// </summary>
    /// <param name="providerId">The provider being evaluated.</param>
    /// <param name="deadlineUtc">The recorded deadline, or <see langword="null"/> when no penalty is active.</param>
    /// <returns><see langword="true"/> when the refresh must be skipped; otherwise, <see langword="false"/>.</returns>
    public static bool IsBlocked(string providerId, DateTimeOffset? deadlineUtc)
    {

        var nowUtc = DateTimeOffset.UtcNow;

        if (RateLimitPolicy.CanDispatch(nowUtc, deadlineUtc))
            return false;

        Log.Debug(
            "Skipping {ProviderId} refresh: rate-limit deadline active until {DeadlineUtc:O}, {RemainingSeconds}s remaining",
            providerId,
            deadlineUtc,
            (int)(deadlineUtc!.Value - nowUtc).TotalSeconds
        );

        return true;
    }

    /// <summary>
    /// Records the rate-limit transition between the previous and the incoming snapshot. Recovery is reported only for a
    /// successful reading: a fault after a penalty produces a stale snapshot that still carries the previous deadline forward,
    /// which is not a recovery.
    /// </summary>
    /// <param name="previous">The snapshot being replaced, or <see langword="null"/> on the first reading.</param>
    /// <param name="current">The snapshot about to be stored.</param>
    public static void LogTransition(Snapshot? previous, Snapshot current)
    {

        if (current.Status == ProviderStatus.RateLimited)
        {

            LogRateLimited(current);

            return;
        }

        if (previous?.Status != ProviderStatus.RateLimited || current.Status != ProviderStatus.Ok)
            return;

        Log.Information(
            "Provider {ProviderId} recovered from rate limit with status {Status}",
            current.ProviderId,
            current.Status
        );
    }

    private static void LogRateLimited(Snapshot snapshot)
    {

        var deadlineUtc = snapshot.ActiveBlock?.ResetTimeUtc;
        var penalty = deadlineUtc.HasValue ? deadlineUtc.Value - snapshot.FetchedAtUtc : TimeSpan.Zero;

        Log.Warning(
            "Provider {ProviderId} rate limited: backing off {PenaltySeconds}s, server Retry-After {RetryAfterSeconds}, next attempt at {DeadlineUtc:O}",
            snapshot.ProviderId,
            (int)penalty.TotalSeconds,
            snapshot.ActiveBlock?.RetryAfterSeconds,
            deadlineUtc
        );
    }
}
