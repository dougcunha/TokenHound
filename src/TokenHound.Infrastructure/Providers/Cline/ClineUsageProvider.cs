using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Adapts borrowed Cline account telemetry and local session evidence into domain snapshots.
/// </summary>
/// <remarks>
/// Cline publishes no token quota, so the provider never invents one: the credit balance is reported
/// as a remaining value, the free model limit is reported only when the CLI recorded it locally, and
/// the Cline Pass windows report a fraction only from the published caps and sampled usage.
/// </remarks>
public sealed partial class ClineUsageProvider : IUsageProvider, IDisposable
{
    /// <summary>The unique provider identifier for Cline.</summary>
    public const string PROVIDER_ID = "cline";

    /// <summary>The guidance message prompting the user to authenticate with Cline.</summary>
    public const string NEEDS_AUTH_MESSAGE = "Run the Cline CLI or app and sign in to lend TokenHound a session.";

    /// <summary>The guidance message shown when the borrowed access token has already expired.</summary>
    public const string TOKEN_EXPIRED_MESSAGE
        = "Cline access token expired; use Cline once so it refreshes the borrowed token.";

    /// <summary>The active block reason when the free model tier is exhausted.</summary>
    public const string FREE_LIMIT_REASON = "FreeModelLimitReached";

    private const int MAX_CONSECUTIVE_RATE_LIMITS = 10;

    private readonly Random? _backoffJitter;
    private readonly ClineAccountClient _client;
    private readonly bool _disposeClient;
    private readonly ClineCredentialDiscovery _discovery;
    private readonly ClineLocalSessionReader _localReader;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly TimeProvider _timeProvider;

    private UsageBlock? _activeRateLimitBlock;
    private int _consecutiveRateLimits;
    private Snapshot? _lastSuccessfulSnapshot;
    private DateTimeOffset? _rateLimitDeadlineUtc;
    private string? _userId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineUsageProvider"/> class.
    /// </summary>
    /// <param name="discovery">An optional credential discovery for the borrowed session.</param>
    /// <param name="client">An optional pre-configured account client.</param>
    /// <param name="localReader">An optional reader for the local session artifacts.</param>
    /// <param name="timeProvider">An optional clock for deadlines and windows.</param>
    /// <param name="rateLimitPolicy">An optional rate-limit policy for 429 deadlines.</param>
    /// <param name="backoffJitter">An optional deterministic random source for deadline jitter.</param>
    /// <param name="clientFactory">An optional factory used to create the account client on demand.</param>
    public ClineUsageProvider(
        ClineCredentialDiscovery? discovery = null,
        ClineAccountClient? client = null,
        ClineLocalSessionReader? localReader = null,
        TimeProvider? timeProvider = null,
        RateLimitPolicy? rateLimitPolicy = null,
        Random? backoffJitter = null,
        Func<ClineAccountClient>? clientFactory = null)
    {

        _timeProvider = timeProvider ?? TimeProvider.System;
        _discovery = discovery ?? new ClineCredentialDiscovery();
        _localReader = localReader ?? new ClineLocalSessionReader(timeProvider: _timeProvider);
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
        _backoffJitter = backoffJitter;

        (_client, _disposeClient) = client is not null
            ? (client, false)
            : clientFactory is not null
                ? (clientFactory(), false)
                : (new ClineAccountClient(timeProvider: _timeProvider), true);
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

        if (credential is null)
            return CreateNeedsAuthSnapshot(nowUtc);

        if (credential.HasExpired(nowUtc))
            return CreateNeedsAuthSnapshot(nowUtc, TOKEN_EXPIRED_MESSAGE);

        var local = await _localReader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return await FetchSnapshotAsync(credential, local, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _client.Dispose();
    }
}