using System;
using System.Collections.Generic;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Providers;

/// <summary>
/// Builds the rate-limited snapshot shared by HTTP-backed providers.
/// </summary>
internal static class RateLimitedSnapshotFactory
{
    /// <summary>
    /// Creates a rate-limited snapshot and the updated consecutive rate-limit count.
    /// </summary>
    /// <param name="providerId">The owning provider identifier.</param>
    /// <param name="reason">The block reason reported by the provider.</param>
    /// <param name="retryAfterSeconds">The server-reported Retry-After value, if any.</param>
    /// <param name="timeProvider">The clock used for snapshot and deadline timestamps.</param>
    /// <param name="rateLimitPolicy">The policy calculating the retry deadline.</param>
    /// <param name="backoffJitter">The optional jitter source for rate-limit deadlines.</param>
    /// <param name="consecutiveRateLimits">The current consecutive rate-limit count.</param>
    /// <param name="maxConsecutiveRateLimits">The ceiling applied to the consecutive count.</param>
    /// <param name="limitWindows">The limit windows to preserve, or <see langword="null"/> for none.</param>
    /// <returns>The rate-limited snapshot and the updated consecutive rate-limit count.</returns>
    internal static (Snapshot Snapshot, int ConsecutiveCount) Create(
        string providerId,
        string reason,
        int? retryAfterSeconds,
        TimeProvider timeProvider,
        RateLimitPolicy rateLimitPolicy,
        Random? backoffJitter,
        int consecutiveRateLimits,
        int maxConsecutiveRateLimits,
        IReadOnlyList<LimitWindow>? limitWindows = null)
    {

        var nowUtc = timeProvider.GetUtcNow();
        var updatedCount = Math.Min(consecutiveRateLimits + 1, maxConsecutiveRateLimits);
        var deadline = rateLimitPolicy.CalculateDeadline(
            nowUtc,
            retryAfterSeconds,
            updatedCount,
            backoffJitter
        );
        var activeBlock = CreateActiveBlock(reason, retryAfterSeconds, deadline);
        var snapshot = BuildSnapshot(
            providerId,
            nowUtc,
            limitWindows,
            activeBlock
        );

        return (snapshot, updatedCount);
    }

    private static UsageBlock CreateActiveBlock(
        string reason,
        int? retryAfterSeconds,
        DateTimeOffset deadline)
        => new()
        {
            Reason = reason,
            IsBlocked = true,
            ResetTimeUtc = deadline,
            RetryAfterSeconds = retryAfterSeconds
        };

    private static Snapshot BuildSnapshot(
        string providerId,
        DateTimeOffset nowUtc,
        IReadOnlyList<LimitWindow>? limitWindows,
        UsageBlock activeBlock)
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = limitWindows ?? [],
            ActiveBlock = activeBlock
        };
}
