using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Adapts OpenCode Go quota telemetry into authoritative domain snapshots.
/// </summary>
public sealed class OpenCodeUsageProvider : IUsageProvider, IDisposable
{
    /// <summary>The unique provider identifier for OpenCode.</summary>
    public const string PROVIDER_ID = "opencode";

    /// <summary>The guidance message prompting the user to authenticate.</summary>
    public const string NEEDS_AUTH_MESSAGE = "Execute 'opencode /connect' in terminal to authenticate.";

    /// <summary>The error message returned when an OpenCode Go subscription is missing.</summary>
    public const string ACCESS_DENIED_MESSAGE = "OpenCode Go subscription required";

    /// <summary>The active block reason when rate-limited.</summary>
    public const string RATE_LIMIT_REASON = "RateLimitReached";

    private const string ROLLING_WINDOW_NAME = "5-Hour Rolling";
    private const string WEEKLY_WINDOW_NAME = "Weekly";
    private const string MONTHLY_WINDOW_NAME = "Monthly";
    private const string GROUP_NAME = "OpenCode Go";
    private const string RATE_LIMITED_STATUS = "rate-limited";
    private const int MAX_CONSECUTIVE_RATE_LIMITS = 10;

    private readonly OpenCodeCredentialDiscovery _discovery;
    private readonly OpenCodeApiClient _client;
    private readonly bool _disposeClient;
    private readonly TimeProvider _timeProvider;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly Random? _backoffJitter;

    private Snapshot? _lastSuccessfulSnapshot;
    private DateTimeOffset? _rateLimitDeadlineUtc;
    private UsageBlock? _activeRateLimitBlock;
    private int _consecutiveRateLimits;

    /// <summary>Initializes a new instance of the <see cref="OpenCodeUsageProvider"/> class.</summary>
    public OpenCodeUsageProvider(
        OpenCodeCredentialDiscovery? discovery = null,
        OpenCodeApiClient? client = null,
        TimeProvider? timeProvider = null,
        RateLimitPolicy? rateLimitPolicy = null,
        Random? backoffJitter = null,
        Func<OpenCodeApiClient>? clientFactory = null)
    {

        _timeProvider = timeProvider ?? TimeProvider.System;
        _discovery = discovery ?? new OpenCodeCredentialDiscovery();
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
        _backoffJitter = backoffJitter;

        (_client, _disposeClient) = client is not null
            ? (client, false)
            : clientFactory is not null
                ? (clientFactory(), false)
                : (new OpenCodeApiClient(timeProvider: _timeProvider), true);
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = _timeProvider.GetUtcNow();

        if (!RateLimitPolicy.CanDispatch(nowUtc, _rateLimitDeadlineUtc))
            return CreateRateLimitedSnapshot(nowUtc);

        var credential = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);

        if (credential is null || string.IsNullOrWhiteSpace(credential.ApiKey))
            return CreateNeedsAuthSnapshot(nowUtc);

        return await FetchSnapshotAsync(credential.ApiKey, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _client.Dispose();
    }

    private async ValueTask<Snapshot> FetchSnapshotAsync(string apiKey, CancellationToken cancellationToken)
    {

        try
        {

            var response = await _client.GetUsageAsync(apiKey, cancellationToken).ConfigureAwait(false);

            var rateLimitedWindow = FindRateLimitedWindow(response.Usage);

            if (rateLimitedWindow is not null)
                return HandleRateLimit(null, RATE_LIMIT_REASON, rateLimitedWindow.ResetsAt);

            _consecutiveRateLimits = 0;
            _rateLimitDeadlineUtc = null;
            _activeRateLimitBlock = null;
            _lastSuccessfulSnapshot = CreateOkSnapshot(response, _timeProvider.GetUtcNow());

            return _lastSuccessfulSnapshot;
        }
        catch (HttpRequestException ex)
        {

            return MapFailure(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            return CreateStaleSnapshot(_timeProvider.GetUtcNow(), ex.Message);
        }
    }

    private Snapshot MapFailure(HttpRequestException ex)
        => SnapshotFailureMapper.Classify(ex.StatusCode, hasCredential: true) switch
        {
            SnapshotFailureMapper.Outcome.RateLimited => ex is OpenCodeRateLimitException rateLimitEx
                ? HandleRateLimit(rateLimitEx.RetryAfterSeconds, rateLimitEx.Message, rateLimitEx.ResetTimeUtc)
                : HandleRateLimit(null, ex.Message),
            SnapshotFailureMapper.Outcome.NeedsAuth => CreateNeedsAuthSnapshot(_timeProvider.GetUtcNow(), ex.Message),
            SnapshotFailureMapper.Outcome.AccessDenied => CreateAccessDeniedSnapshot(_timeProvider.GetUtcNow()),
            _ => CreateStaleSnapshot(_timeProvider.GetUtcNow(), ex.Message)
        };

    private Snapshot HandleRateLimit(
        int? retryAfterSeconds,
        string? message,
        DateTimeOffset? authoritativeResetTimeUtc = null)
    {

        var nowUtc = _timeProvider.GetUtcNow();
        var hasValidRetryAfter = retryAfterSeconds is >= 0;
        DateTimeOffset? resetTimeUtc = !hasValidRetryAfter &&
            authoritativeResetTimeUtc is { } candidate &&
            candidate > nowUtc
                ? candidate
                : null;
        var retrySeconds = hasValidRetryAfter
            ? retryAfterSeconds
            : resetTimeUtc is null
                ? ResolveRetrySeconds(null, nowUtc)
                : null;

        _consecutiveRateLimits = Math.Min(_consecutiveRateLimits + 1, MAX_CONSECUTIVE_RATE_LIMITS);

        var deadline = CalculateRateLimitDeadline(nowUtc, retrySeconds, resetTimeUtc);

        _rateLimitDeadlineUtc = deadline;

        _activeRateLimitBlock = new UsageBlock
        {
            Reason = RATE_LIMIT_REASON,
            IsBlocked = true,
            ResetTimeUtc = deadline,
            RetryAfterSeconds = hasValidRetryAfter ? retryAfterSeconds : retrySeconds
        };

        Log.Warning("OpenCode rate-limited until {DeadlineUtc:O}", deadline);

        return CreateRateLimitedSnapshot(nowUtc, message);
    }

    private DateTimeOffset CalculateRateLimitDeadline(
        DateTimeOffset nowUtc,
        int? retrySeconds,
        DateTimeOffset? authoritativeResetTimeUtc)
    {

        var calculatedDeadline = _rateLimitPolicy.CalculateDeadline(
            nowUtc,
            retrySeconds,
            _consecutiveRateLimits,
            _backoffJitter
        );

        if (authoritativeResetTimeUtc is { } resetTimeUtc && resetTimeUtc > calculatedDeadline)
            return resetTimeUtc;

        return calculatedDeadline;
    }

    private int? ResolveRetrySeconds(int? retryAfterSeconds, DateTimeOffset nowUtc)
    {

        if (retryAfterSeconds.HasValue && retryAfterSeconds.Value >= 0)
            return retryAfterSeconds.Value;

        var window = _lastSuccessfulSnapshot?.LimitWindows
            .FirstOrDefault(static w => string.Equals(w.Name, ROLLING_WINDOW_NAME, StringComparison.OrdinalIgnoreCase));

        if (window?.ResetTimeUtc is { } resetTime && resetTime > nowUtc)
            return (int)Math.Ceiling((resetTime - nowUtc).TotalSeconds);

        return null;
    }

    private static Snapshot CreateOkSnapshot(OpenCodeUsageResponse response, DateTimeOffset fetchedAtUtc)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = fetchedAtUtc,
            LimitWindows = MapLimitWindows(response.Usage),
            ActiveBlock = null,
            ErrorDescription = null
        };

    private static OpenCodeLimitWindowDto? FindRateLimitedWindow(OpenCodeUsageData usage)
    {

        // Keep polling blocked until every rate-limited window has reset.
        return new[] { usage.Rolling, usage.Weekly, usage.Monthly }
            .Where(static window => string.Equals(window.Status, RATE_LIMITED_STATUS, StringComparison.OrdinalIgnoreCase))
            .MaxBy(static window => window.ResetsAt);
    }

    private static IReadOnlyList<LimitWindow> MapLimitWindows(OpenCodeUsageData usage)
        =>
        [
            CreateWindow(
                ROLLING_WINDOW_NAME,
                TimeSpan.FromHours(5),
                usage.Rolling.Percent,
                usage.Rolling.ResetsAt
            ),
            CreateWindow(
                WEEKLY_WINDOW_NAME,
                TimeSpan.FromDays(7),
                usage.Weekly.Percent,
                usage.Weekly.ResetsAt
            ),
            CreateWindow(
                MONTHLY_WINDOW_NAME,
                TimeSpan.FromDays(30),
                usage.Monthly.Percent,
                usage.Monthly.ResetsAt
            )
        ];

    private static LimitWindow CreateWindow(
        string name,
        TimeSpan period,
        double percent,
        DateTimeOffset resetsAt)
        => new()
        {
            Name = name,
            GroupName = GROUP_NAME,
            Period = period,
            TotalUnits = 100L,
            RemainingUnits = (long)Math.Max(0, Math.Round(100.0 - percent)),
            UsedFraction = Math.Clamp(percent / 100.0, 0.0, 1.0),
            ResetTimeUtc = resetsAt
        };

    private Snapshot CreateRateLimitedSnapshot(DateTimeOffset nowUtc, string? errorDescription = null)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.RateLimited,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ActiveBlock = _activeRateLimitBlock,
            ErrorDescription = errorDescription ?? _activeRateLimitBlock?.Reason
        };

    private static Snapshot CreateNeedsAuthSnapshot(DateTimeOffset nowUtc, string? errorDescription = null)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = errorDescription ?? NEEDS_AUTH_MESSAGE
        };

    private static Snapshot CreateAccessDeniedSnapshot(DateTimeOffset nowUtc)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.AccessDenied,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = ACCESS_DENIED_MESSAGE
        };

    private Snapshot CreateStaleSnapshot(DateTimeOffset nowUtc, string description)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ActiveBlock = _activeRateLimitBlock,
            ErrorDescription = description
        };
}
