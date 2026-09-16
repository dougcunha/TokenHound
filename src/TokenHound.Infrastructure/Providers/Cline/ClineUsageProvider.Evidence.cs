using Serilog;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Resolves the borrowed Cline account and local evidence backing each snapshot.
/// </summary>
public sealed partial class ClineUsageProvider
{
    private async ValueTask<Snapshot> FetchSnapshotAsync(
        ClineAuthDto credential,
        ClineLocalUsageSample local,
        CancellationToken cancellationToken)
    {

        var nowUtc = _timeProvider.GetUtcNow();

        try
        {

            var user = await ResolveUserAsync(credential, cancellationToken).ConfigureAwait(false);

            if (user?.Id is not { } userId)
                return CreateNeedsAuthSnapshot(nowUtc);

            var balance = await _client.GetBalanceAsync(
                credential.AccessToken,
                userId,
                cancellationToken
            ).ConfigureAwait(false);
            var plan = await _client.GetPlanAsync(credential.AccessToken, cancellationToken).ConfigureAwait(false);
            var windows = await ResolvePassWindowsAsync(credential, userId, plan, cancellationToken).ConfigureAwait(false);

            _consecutiveRateLimits = 0;
            _rateLimitDeadlineUtc = null;
            _activeRateLimitBlock = null;
            _lastSuccessfulSnapshot = CreateOkSnapshot(
                nowUtc,
                new ClineEvidence(CreateAccountUsage(balance, plan), windows, local)
            );

            return _lastSuccessfulSnapshot;
        }
        catch (HttpRequestException ex)
        {

            return MapFailure(ex, local);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            return CreateStaleSnapshot(_timeProvider.GetUtcNow(), ex.Message, ToEvidence(local));
        }
    }

    private async Task<ClineAccountUser?> ResolveUserAsync(
        ClineAuthDto credential,
        CancellationToken cancellationToken)
    {

        if (!string.IsNullOrWhiteSpace(_userId))
            return new ClineAccountUser { Id = _userId };

        var user = await _client.GetUserAsync(credential.AccessToken, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(user?.Id))
            _userId = user.Id;

        return user;
    }

    private async Task<IReadOnlyList<LimitWindow>> ResolvePassWindowsAsync(
        ClineAuthDto credential,
        string userId,
        ClineCurrentPlan? plan,
        CancellationToken cancellationToken)
    {

        var caps = ExtractCaps(plan);

        if (caps is null)
            return [];

        var transactions = await _client.GetUsageAsync(
            credential.AccessToken,
            userId,
            cancellationToken
        ).ConfigureAwait(false);

        return ClinePassWindowMapper.Map(caps, transactions, _timeProvider.GetUtcNow());
    }

    private static ClineInferenceCapThreshold? ExtractCaps(ClineCurrentPlan? plan)
    {

        var entitlement = plan?.Entitlements?.ClinePass ?? plan?.Plan?.Entitlements?.ClinePass;

        if (entitlement?.Enabled != true)
            return null;

        return entitlement.InferenceCapThreshold;
    }

    private static ClineAccountUsage CreateAccountUsage(
        ClineAccountBalance? balance,
        ClineCurrentPlan? plan)
        => new()
        {
            BalanceCredits = balance?.Balance,
            PlanName = plan?.Plan?.DisplayName ?? plan?.Plan?.Interval,
            HasPassSubscription = ExtractCaps(plan) is not null
        };

    private static ClineEvidence ToEvidence(ClineLocalUsageSample local)
        => new(
            null,
            [],
            local
        );

    private Snapshot MapFailure(HttpRequestException ex, ClineLocalUsageSample local)
        => SnapshotFailureMapper.Classify(ex.StatusCode, hasCredential: true) switch
        {
            SnapshotFailureMapper.Outcome.RateLimited => ex is ClineRateLimitException rateLimitEx
                ? HandleRateLimit(rateLimitEx.RetryAfterSeconds, rateLimitEx.Message, ToEvidence(local))
                : HandleRateLimit(null, ex.Message, ToEvidence(local)),
            SnapshotFailureMapper.Outcome.NeedsAuth => CreateNeedsAuthSnapshot(_timeProvider.GetUtcNow(), ex.Message),
            SnapshotFailureMapper.Outcome.AccessDenied => CreateAccessDeniedSnapshot(_timeProvider.GetUtcNow()),
            _ => CreateStaleSnapshot(_timeProvider.GetUtcNow(), ex.Message, ToEvidence(local))
        };

    private Snapshot HandleRateLimit(
        int? retryAfterSeconds,
        string? message,
        ClineEvidence evidence)
    {

        var nowUtc = _timeProvider.GetUtcNow();
        _consecutiveRateLimits = Math.Min(_consecutiveRateLimits + 1, MAX_CONSECUTIVE_RATE_LIMITS);

        var deadline = _rateLimitPolicy.CalculateDeadline(
            nowUtc,
            ResolveRetrySeconds(retryAfterSeconds),
            _consecutiveRateLimits,
            _backoffJitter
        );

        _rateLimitDeadlineUtc = deadline;
        _activeRateLimitBlock = new UsageBlock
        {
            Reason = "RateLimitReached",
            IsBlocked = true,
            ResetTimeUtc = deadline,
            RetryAfterSeconds = retryAfterSeconds is >= 0 ? retryAfterSeconds : null
        };

        Log.Warning("Cline rate-limited until {DeadlineUtc:O}", deadline);

        return CreateRateLimitedSnapshot(nowUtc, evidence, message);
    }

    private int? ResolveRetrySeconds(int? retryAfterSeconds)
    {

        if (retryAfterSeconds is >= 0)
            return retryAfterSeconds;

        return null;
    }
}