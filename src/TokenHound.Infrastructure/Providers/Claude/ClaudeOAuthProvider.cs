using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Adapts Claude Code OAuth discovery and usage telemetry into domain snapshots.
/// </summary>
public sealed class ClaudeOAuthProvider : IUsageProvider
{
    /// <summary>
    /// The unique provider identifier for Claude Code.
    /// </summary>
    public const string PROVIDER_ID = "claude";

    /// <summary>
    /// The standard error message prompting the user to authenticate in the terminal.
    /// </summary>
    public const string NEEDS_AUTH_MESSAGE = "Execute 'claude login' in terminal";

    /// <summary>
    /// The identifier for the five-hour rolling session limit window.
    /// </summary>
    public const string FIVE_HOUR_WINDOW_NAME = "five_hour";

    /// <summary>
    /// The identifier for the seven-day quota limit window.
    /// </summary>
    public const string SEVEN_DAY_WINDOW_NAME = "seven_day";

    /// <summary>
    /// The ceiling applied to the consecutive rate-limit counter feeding the exponential backoff.
    /// </summary>
    private const int MAX_CONSECUTIVE_RATE_LIMITS = 10;

    private readonly string _providerId;
    private readonly string? _credentialsFilePath;
    private readonly ClaudeProfileDiscovery _discovery;
    private readonly ClaudeOAuthClient _client;
    private readonly Random? _backoffJitter;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private Snapshot? _lastSuccessfulSnapshot;
    private int _consecutiveRateLimits;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeOAuthProvider"/> class.
    /// </summary>
    /// <param name="discovery">The profile discovery service, or <see langword="null"/> to use default discovery.</param>
    /// <param name="client">The OAuth usage API client, or <see langword="null"/> to use default client.</param>
    /// <param name="backoffJitter">The generator producing backoff jitter, or <see langword="null"/> to use <see cref="Random.Shared"/>.</param>
    /// <param name="rateLimitPolicy">The isolated retry policy, or <see langword="null"/> to use the default floor.</param>
    /// <param name="providerId">The unique provider identifier, or <see langword="null"/> to use default <see cref="PROVIDER_ID"/>.</param>
    /// <param name="credentialsFilePath">An explicit path to the credentials JSON file, or <see langword="null"/> to use discovery.</param>
    public ClaudeOAuthProvider(
        ClaudeProfileDiscovery? discovery = null,
        ClaudeOAuthClient? client = null,
        Random? backoffJitter = null,
        RateLimitPolicy? rateLimitPolicy = null,
        string? providerId = null,
        string? credentialsFilePath = null)
    {

        _providerId = string.IsNullOrWhiteSpace(providerId) ? PROVIDER_ID : providerId;
        _credentialsFilePath = credentialsFilePath;
        _discovery = discovery ?? new ClaudeProfileDiscovery();
        _client = client ?? new ClaudeOAuthClient();
        _backoffJitter = backoffJitter;
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
    }

    /// <inheritdoc />
    public string ProviderId
        => _providerId;

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var credential = _credentialsFilePath is not null
            ? await ClaudeProfileDiscovery.LoadCredentialFromFileAsync(_credentialsFilePath, cancellationToken).ConfigureAwait(false)
            : await _discovery.DiscoverCredentialAsync(cancellationToken).ConfigureAwait(false);

        if (credential is null || string.IsNullOrWhiteSpace(credential.AccessToken) || credential.IsExpired)
            return CreateNeedsAuthSnapshot();

        return await FetchSnapshotAsync(credential.AccessToken, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<Snapshot> FetchSnapshotAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {

        try
        {

            var usage = await _client.GetUsageAsync(accessToken, cancellationToken).ConfigureAwait(false);
            var snapshot = CreateOkSnapshot(usage);

            _lastSuccessfulSnapshot = snapshot;
            _consecutiveRateLimits = 0;

            return snapshot;
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (ClaudeOAuthClient.RateLimitException ex)
        {

            return CreateRateLimitedSnapshot(ex);
        }
        catch (HttpRequestException ex)
        {

            return HandleHttpException(ex);
        }
        catch (Exception ex)
        {

            return CreateDegradedSnapshot(ex);
        }
    }

    private Snapshot HandleHttpException(HttpRequestException ex)
    {

        if (ex.StatusCode == HttpStatusCode.TooManyRequests)
            return CreateRateLimitedSnapshot(ex);

        if (ex.StatusCode == HttpStatusCode.Unauthorized)
            return CreateNeedsAuthSnapshot();

        return CreateDegradedSnapshot(ex);
    }

    private Snapshot CreateNeedsAuthSnapshot()
    {

        return new Snapshot
        {
            ProviderId = _providerId,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = NEEDS_AUTH_MESSAGE
        };
    }

    private Snapshot CreateOkSnapshot(ClaudeUsageResponse? usage)
    {

        var windows = ClaudeQuotaWindowMapper.Map(usage);

        return new Snapshot
        {
            ProviderId = _providerId,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows,
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private Snapshot CreateRateLimitedSnapshot(ClaudeOAuthClient.RateLimitException ex)
        => CreateRateLimitedSnapshot(ex.Message, ex.RetryAfterSeconds);

    private Snapshot CreateRateLimitedSnapshot(HttpRequestException ex)
        => CreateRateLimitedSnapshot(ex.Message, null);

    private Snapshot CreateRateLimitedSnapshot(string reason, int? retryAfterSeconds)
    {

        var (snapshot, consecutiveRateLimits) = RateLimitedSnapshotFactory.Create(
            _providerId,
            reason,
            retryAfterSeconds,
            TimeProvider.System,
            _rateLimitPolicy,
            _backoffJitter,
            _consecutiveRateLimits,
            MAX_CONSECUTIVE_RATE_LIMITS,
            _lastSuccessfulSnapshot?.LimitWindows
        );

        _consecutiveRateLimits = consecutiveRateLimits;

        return snapshot;
    }

    private Snapshot CreateDegradedSnapshot(Exception ex)
    {

        return new Snapshot
        {
            ProviderId = _providerId,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ActiveBlock = null,
            ErrorDescription = ex.Message
        };
    }
}
