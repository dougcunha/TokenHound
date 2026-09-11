using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Implements the usage provider for Google Antigravity / Gemini with multi-tier fallback.
/// </summary>
public sealed partial class AntigravityUsageProvider : IUsageProvider, IDisposable
{
    private const string PROVIDER_ID = "gemini";
    private const int MAX_CONSECUTIVE_RATE_LIMITS = 10;

    private readonly AntigravityEndpointDiscovery _discovery;
    private readonly AntigravityLanguageServerClient _client;
    private readonly AntigravityCloudCodeClient _cloudCodeClient;
    private readonly AntigravityTranscriptReader _transcriptReader;
    private readonly bool _disposeClient;
    private readonly bool _disposeCloudCodeClient;
    private readonly TimeProvider _timeProvider;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly Random? _backoffJitter;

    private int _consecutiveRateLimits;

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityUsageProvider"/> class.
    /// </summary>
    /// <param name="discovery">Optional endpoint discovery instance.</param>
    /// <param name="client">Optional language server client.</param>
    /// <param name="transcriptReader">Optional transcript reader instance.</param>
    /// <param name="cloudCodeClient">Optional Cloud Code client instance.</param>
    /// <param name="timeProvider">Optional clock for snapshot and deadline timestamps.</param>
    /// <param name="rateLimitPolicy">Optional isolated retry policy.</param>
    /// <param name="backoffJitter">Optional jitter source for remote rate-limit deadlines.</param>
    public AntigravityUsageProvider(
        AntigravityEndpointDiscovery? discovery = null,
        AntigravityLanguageServerClient? client = null,
        AntigravityTranscriptReader? transcriptReader = null,
        AntigravityCloudCodeClient? cloudCodeClient = null,
        TimeProvider? timeProvider = null,
        RateLimitPolicy? rateLimitPolicy = null,
        Random? backoffJitter = null)
    {
        _discovery = discovery ?? new AntigravityEndpointDiscovery();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
        _backoffJitter = backoffJitter;

        (_client, _disposeClient) = ResolveLanguageServerClient(client);
        (_cloudCodeClient, _disposeCloudCodeClient) = ResolveCloudCodeClient(cloudCodeClient);

        _transcriptReader = transcriptReader ?? new AntigravityTranscriptReader();
    }

    private static (AntigravityLanguageServerClient Client, bool ShouldDispose) ResolveLanguageServerClient(
        AntigravityLanguageServerClient? client)
        => client is not null
            ? (client, false)
            : (new AntigravityLanguageServerClient(), true);

    private static (AntigravityCloudCodeClient Client, bool ShouldDispose) ResolveCloudCodeClient(
        AntigravityCloudCodeClient? client)
        => client is not null
            ? (client, false)
            : (new AntigravityCloudCodeClient(), true);

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var (officialSnapshot, isRunning) = await TryGetLanguageServerSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (officialSnapshot is not null)
        {
            return officialSnapshot;
        }

        var (cloudCodeSnapshot, cloudCodeFailure) = await GetCloudCodeOutcomeAsync(cancellationToken).ConfigureAwait(false);

        if (cloudCodeSnapshot is not null)
        {
            return cloudCodeSnapshot;
        }

        if (cloudCodeFailure?.Status == ProviderStatus.NeedsAuth && isRunning)
            return cloudCodeFailure;

        var derivedSnapshot = await TryGetTranscriptSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (derivedSnapshot is not null)
            return cloudCodeFailure?.ActiveBlock is { } activeBlock
                ? derivedSnapshot with { ActiveBlock = activeBlock }
                : derivedSnapshot;

        return ResolveUnavailableSnapshot(isRunning, cloudCodeFailure);
    }

    private Snapshot ResolveUnavailableSnapshot(bool isRunning, Snapshot? cloudCodeFailure)
    {
        if (cloudCodeFailure is { Status: not ProviderStatus.NeedsAuth })
            return cloudCodeFailure;

        return isRunning
            ? CreateNeedsAuthSnapshot()
            : CreateNotRunningSnapshot();
    }

    private async ValueTask<(Snapshot? Success, Snapshot? Failure)> GetCloudCodeOutcomeAsync(
        CancellationToken cancellationToken)
    {

        try
        {
            var snapshot = await TryGetCloudCodeSnapshotAsync(cancellationToken).ConfigureAwait(false);

            return (snapshot, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ProviderHttpException ex)
        {
            return (null, MapCloudCodeFailure(ex));
        }
        catch (Exception ex)
        {
            return (null, CreateStaleSnapshot(ex.Message));
        }
    }

    private async ValueTask<(Snapshot? Snapshot, bool IsRunning)> TryGetLanguageServerSnapshotAsync(CancellationToken cancellationToken)
    {
        var endpoint = _discovery.DiscoverEndpoint();

        if (endpoint is null)
        {
            return (null, false);
        }

        var quota = await _client.RetrieveUserQuotaSummaryAsync(endpoint, cancellationToken).ConfigureAwait(false);

        if (quota?.Groups is null || quota.Groups.Count == 0)
        {
            return (null, true);
        }

        var windows = MapQuotaGroups(quota.Groups);

        if (windows.Count == 0)
            return (null, true);

        _consecutiveRateLimits = 0;

        return (CreateOfficialSnapshot(windows), true);
    }

    private async ValueTask<Snapshot?> TryGetCloudCodeSnapshotAsync(CancellationToken cancellationToken)
    {
        var quota = await _cloudCodeClient.RetrieveUserQuotaSummaryAsync(cancellationToken).ConfigureAwait(false);

        if (quota?.Groups is null || quota.Groups.Count == 0)
        {
            return null;
        }

        var windows = MapQuotaGroups(quota.Groups);

        if (windows.Count == 0)
        {
            return null;
        }

        return CreateOfficialSnapshot(windows);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
        {
            _client.Dispose();
        }

        if (_disposeCloudCodeClient)
        {
            _cloudCodeClient.Dispose();
        }
    }
}
