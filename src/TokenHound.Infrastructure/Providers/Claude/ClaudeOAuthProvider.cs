using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

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

    private readonly ClaudeProfileDiscovery _discovery;
    private readonly ClaudeOAuthClient _client;
    private Snapshot? _lastSuccessfulSnapshot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaudeOAuthProvider"/> class.
    /// </summary>
    /// <param name="discovery">The profile discovery service, or <see langword="null"/> to use default discovery.</param>
    /// <param name="client">The OAuth usage API client, or <see langword="null"/> to use default client.</param>
    public ClaudeOAuthProvider(
        ClaudeProfileDiscovery? discovery = null,
        ClaudeOAuthClient? client = null)
    {

        _discovery = discovery ?? new ClaudeProfileDiscovery();
        _client = client ?? new ClaudeOAuthClient();
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var credential = await _discovery.DiscoverCredentialAsync(cancellationToken).ConfigureAwait(false);

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

    private static Snapshot CreateNeedsAuthSnapshot()
    {

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = NEEDS_AUTH_MESSAGE
        };
    }

    private static Snapshot CreateOkSnapshot(ClaudeUsageResponse? usage)
    {

        var windows = MapLimitWindows(usage);

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = windows,
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private static IReadOnlyList<LimitWindow> MapLimitWindows(ClaudeUsageResponse? usage)
    {

        if (usage is null)
            return [];

        var windows = new List<LimitWindow>(2);

        if (usage.FiveHour is not null)
        {

            windows.Add(CreateLimitWindow(
                FIVE_HOUR_WINDOW_NAME,
                usage.FiveHour.Utilization,
                TimeSpan.FromHours(5),
                usage.FiveHour.ResetsAt
            ));
        }

        if (usage.SevenDay is not null)
        {

            windows.Add(CreateLimitWindow(
                SEVEN_DAY_WINDOW_NAME,
                usage.SevenDay.Utilization,
                TimeSpan.FromDays(7),
                usage.SevenDay.ResetsAt
            ));
        }

        return windows;
    }

    private static LimitWindow CreateLimitWindow(
        string name,
        double rawUtilization,
        TimeSpan period,
        DateTimeOffset? resetsAt)
    {

        var fraction = rawUtilization > 1.0
            ? Math.Clamp(rawUtilization / 100.0, 0.0, 1.0)
            : Math.Clamp(rawUtilization, 0.0, 1.0);

        var remaining = (long)Math.Max(0, Math.Round((1.0 - fraction) * 100));

        return new LimitWindow
        {
            Name = name,
            UsedFraction = fraction,
            RemainingUnits = remaining,
            TotalUnits = 100,
            Period = period,
            ResetTimeUtc = resetsAt
        };
    }

    private static Snapshot CreateRateLimitedSnapshot(ClaudeOAuthClient.RateLimitException ex)
    {

        var nowUtc = DateTimeOffset.UtcNow;
        var deadline = RateLimitPolicy.CalculateDeadline(
            nowUtc,
            ex.RetryAfterSeconds,
            1,
            null
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
                Reason = ex.Message,
                IsBlocked = true,
                ResetTimeUtc = deadline,
                RetryAfterSeconds = ex.RetryAfterSeconds
            },
            ErrorDescription = null
        };
    }

    private static Snapshot CreateRateLimitedSnapshot(HttpRequestException ex)
    {

        var nowUtc = DateTimeOffset.UtcNow;
        var deadline = RateLimitPolicy.CalculateDeadline(
            nowUtc,
            null,
            1,
            null
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
                Reason = ex.Message,
                IsBlocked = true,
                ResetTimeUtc = deadline,
                RetryAfterSeconds = null
            },
            ErrorDescription = null
        };
    }

    private Snapshot CreateDegradedSnapshot(Exception ex)
    {

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = _lastSuccessfulSnapshot?.LimitWindows ?? [],
            ActiveBlock = null,
            ErrorDescription = ex.Message
        };
    }
}
