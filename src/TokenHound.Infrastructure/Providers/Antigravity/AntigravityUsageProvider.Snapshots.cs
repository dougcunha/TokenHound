using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Antigravity;

public sealed partial class AntigravityUsageProvider
{
    private static IReadOnlyList<LimitWindow> MapQuotaGroups(IReadOnlyList<AntigravityGroupDto> groups)
    {
        var windows = new List<LimitWindow>();

        foreach (var group in groups)
        {

            if (group.Buckets is null)
                continue;

            foreach (var bucket in group.Buckets)
            {
                var usedFraction = AntigravityLanguageServerClient.CalculateUsedFraction(bucket.RemainingFraction);
                long? remainingUnits = bucket.RemainingFraction.HasValue
                    ? (long)Math.Round(bucket.RemainingFraction.Value * 100.0)
                    : null;

                windows.Add(CreateLimitWindow(group, bucket, usedFraction, remainingUnits));
            }
        }

        return windows;
    }

    private static LimitWindow CreateLimitWindow(
        AntigravityGroupDto group,
        AntigravityBucketDto bucket,
        double? usedFraction,
        long? remainingUnits)
        => new()
        {
            Name = bucket.DisplayName ?? group.DisplayName ?? bucket.BucketId ?? "Quota Window",
            GroupName = group.DisplayName,
            Period = ResolveBucketPeriod(bucket),
            TotalUnits = usedFraction.HasValue ? 100 : null,
            UsedFraction = usedFraction,
            RemainingUnits = remainingUnits,
            ResetTimeUtc = bucket.ResetTime
        };

    private static TimeSpan? ResolveBucketPeriod(AntigravityBucketDto bucket)
        => bucket.Window switch
        {
            "5h" => TimeSpan.FromHours(5),
            "weekly" => TimeSpan.FromDays(7),
            _ => bucket.DisplayName?.Contains("five", StringComparison.OrdinalIgnoreCase) == true
                ? TimeSpan.FromHours(5)
                : bucket.DisplayName?.Contains("week", StringComparison.OrdinalIgnoreCase) == true
                    ? TimeSpan.FromDays(7)
                    : null
        };

    private static Snapshot CreateOfficialSnapshot(IReadOnlyList<LimitWindow> windows)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows,
            ActiveBlock = null,
            ErrorDescription = null
        };

    private async ValueTask<Snapshot?> TryGetTranscriptSnapshotAsync(CancellationToken cancellationToken)
    {
        var hasTranscripts = _transcriptReader.HasAnyTranscripts();
        var hasCredentials = await _cloudCodeClient.HasCredentialAsync(cancellationToken).ConfigureAwait(false);

        if (!hasTranscripts && !hasCredentials)
            return null;

        var requestsToday = await _transcriptReader.CountTodayModelRequestsAsync(cancellationToken).ConfigureAwait(false);

        return CreateTranscriptSnapshot(requestsToday);
    }

    private Snapshot CreateTranscriptSnapshot(int requestsToday)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows =
            [
                new LimitWindow
                {
                    Name = "Requests Today",
                    TotalUnits = null,
                    UsedFraction = null,
                    RemainingUnits = requestsToday,
                    ResetTimeUtc = null
                }
            ],
            ActiveBlock = null,
            ErrorDescription = null
        };

    private Snapshot CreateNeedsAuthSnapshot()
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = "Launch Antigravity IDE or login to Gemini"
        };

    private Snapshot? MapCloudCodeFailure(ProviderHttpException exception)
        => exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CreateNeedsAuthSnapshot(),
            HttpStatusCode.Forbidden => null,
            HttpStatusCode.TooManyRequests => CreateRateLimitedSnapshot(exception),
            _ => CreateStaleSnapshot(exception.Message)
        };

    private Snapshot CreateRateLimitedSnapshot(ProviderHttpException exception)
    {

        var nowUtc = _timeProvider.GetUtcNow();

        _consecutiveRateLimits = Math.Min(_consecutiveRateLimits + 1, MAX_CONSECUTIVE_RATE_LIMITS);

        var deadline = _rateLimitPolicy.CalculateDeadline(
            nowUtc,
            exception.RetryAfterSeconds,
            _consecutiveRateLimits,
            _backoffJitter
        );

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = new UsageBlock
            {
                Reason = exception.Message,
                IsBlocked = true,
                ResetTimeUtc = deadline,
                RetryAfterSeconds = exception.RetryAfterSeconds
            }
        };
    }

    private Snapshot CreateStaleSnapshot(string description)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [],
            ErrorDescription = description
        };
}
