using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Adapts the Copilot quota and billing endpoints into TokenHound snapshots.
/// </summary>
public sealed partial class CopilotUsageProvider : IUsageProvider, IRequestGatedUsageProvider, IDisposable
{
    /// <summary>The unique provider identifier for Copilot.</summary>
    public const string PROVIDER_ID = "copilot";

    /// <summary>Guidance for restoring a borrowed Copilot OAuth session.</summary>
    public const string NEEDS_AUTH_MESSAGE =
        "Run 'gh auth login' or 'copilot login' in your terminal, then retry. Do not paste a PAT.";

    private const string OVERRIDE_WARNING =
        " Check for an unrelated GH_TOKEN or GITHUB_TOKEN environment override.";

    private readonly CopilotCredentialDiscovery _discovery;
    private readonly CopilotApiClient _client;
    private readonly CopilotRequestGate _gate;
    private readonly CopilotBillingService _billingService;
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;
    private readonly bool _disposeGate;
    private readonly bool _disposeBillingService;

    private string? _currentCredentialToken;
    private CopilotQuotaResponse? _lastQuotaResponse;
    private Snapshot? _lastQuotaSuccessSnapshot;

    /// <summary>Initializes a provider with default read-only discovery, HTTP, gate, and billing dependencies.</summary>
    public CopilotUsageProvider()
        : this(null, null, null, null, null)
    {
    }

    /// <summary>
    /// Initializes a provider with injectable discovery, HTTP client, and clock dependencies.
    /// </summary>
    /// <param name="discovery">Credential discovery, or null for the default.</param>
    /// <param name="client">The API client, or null for a new owned client.</param>
    /// <param name="timeProvider">The clock used for snapshot and deadline timestamps.</param>
    public CopilotUsageProvider(
        CopilotCredentialDiscovery? discovery,
        CopilotApiClient? client,
        TimeProvider? timeProvider = null)
        : this(discovery, client, null, null, timeProvider)
    {
    }

    /// <summary>
    /// Initializes a provider with all optional dependencies for testing and composition.
    /// </summary>
    /// <param name="discovery">Credential discovery, or null for the default.</param>
    /// <param name="client">The API client, or null for a new owned client.</param>
    /// <param name="gate">The Copilot request gate, or null for a new owned gate.</param>
    /// <param name="billingService">The billing service, or null for a new owned service.</param>
    /// <param name="timeProvider">The clock used for timestamps.</param>
    public CopilotUsageProvider(
        CopilotCredentialDiscovery? discovery,
        CopilotApiClient? client,
        CopilotRequestGate? gate,
        CopilotBillingService? billingService,
        TimeProvider? timeProvider = null)
    {

        _discovery = discovery ?? new CopilotCredentialDiscovery();
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (gate is null)
        {
            var archive = new UsageArchive();
            _gate = new CopilotRequestGate(archive, _timeProvider);
            _disposeGate = true;
        }
        else
        {
            _gate = gate;
            _disposeGate = false;
        }

        if (client is null)
        {
            _client = new CopilotApiClient();
            _disposeClient = true;
        }
        else
        {
            _client = client;
            _disposeClient = false;
        }

        if (billingService is null)
        {
            var billingClient = new CopilotBillingClient(gate: _gate);
            var resolver = new CopilotBillingContextResolver(billingClient);
            var billingArchive = new UsageArchive();
            _billingService = new CopilotBillingService(billingClient, resolver, billingArchive, _gate, _timeProvider, disposeClient: true);
            _disposeBillingService = true;
        }
        else
        {
            _billingService = billingService;
            _disposeBillingService = false;
        }
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var credential = await _discovery.DiscoverAsync(cancellationToken).ConfigureAwait(false);

        if (credential is null)
        {
            ResetCredentialGeneration(null);
            var needsAuth = CreateNeedsAuthSnapshot();

            return needsAuth with
            {
                CopilotBilling = new CopilotBillingStatus
                {
                    State = CopilotBillingState.Unavailable,
                    Reason = CopilotBillingReason.MissingCredential,
                    AttemptedAtUtc = _timeProvider.GetUtcNow()
                }
            };
        }

        UpdateCredentialGeneration(credential.AccessToken);

        var (quotaSnapshot, quotaResponse) = await FetchQuotaAsync(credential, cancellationToken).ConfigureAwait(false);
        var effectiveQuotaResponse = quotaResponse ?? _lastQuotaResponse;
        var billingStatus = await FetchBillingAsync(credential.AccessToken, effectiveQuotaResponse, cancellationToken).ConfigureAwait(false);

        return quotaSnapshot with { CopilotBilling = billingStatus };
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _client.Dispose();

        if (_disposeBillingService)
            _billingService.Dispose();

        if (_disposeGate)
            _gate.Dispose();
    }

    private async Task<(Snapshot Snapshot, CopilotQuotaResponse? Response)> FetchQuotaAsync(
        CopilotCredential credential,
        CancellationToken cancellationToken)
    {

        try
        {

            var response = await _gate.SendAsync(
                ct => _client.GetQuotaAsync(credential.AccessToken, ct),
                cancellationToken
            ).ConfigureAwait(false);

            _lastQuotaResponse = response;
            var snapshot = MapResponse(response);

            if (snapshot.Status == ProviderStatus.Ok)
                _lastQuotaSuccessSnapshot = snapshot;

            return (snapshot, response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

            throw;
        }
        catch (RateLimitBlockedException ex)
        {

            return (CreateRateLimitedSnapshot(ex.DeadlineUtc, ex.RetryAfterSeconds), null);
        }
        catch (CopilotApiException ex)
        {

            return (MapHttpError(ex, credential), null);
        }
        catch (CopilotTimeoutException)
        {

            return (CreateStaleSnapshot("Copilot quota request timed out."), null);
        }
        catch (JsonException)
        {

            return (CreateStaleSnapshot("Copilot quota response schema was not recognized."), null);
        }
        catch (HttpRequestException)
        {

            return (CreateStaleSnapshot("Copilot quota request failed."), null);
        }
    }

    private async Task<CopilotBillingStatus> FetchBillingAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        CancellationToken cancellationToken)
    {

        try
        {

            return await _billingService.GetBillingStatusAsync(
                accessToken,
                quotaResponse,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {

            throw;
        }
        catch (Exception)
        {

            return new CopilotBillingStatus
            {
                State = CopilotBillingState.Unavailable,
                Reason = CopilotBillingReason.NetworkFailure,
                AttemptedAtUtc = _timeProvider.GetUtcNow()
            };
        }
    }

    private void UpdateCredentialGeneration(string token)
    {

        if (!string.Equals(_currentCredentialToken, token, StringComparison.Ordinal))
            ResetCredentialGeneration(token);
    }

    private void ResetCredentialGeneration(string? token)
    {

        _currentCredentialToken = token;
        _lastQuotaResponse = null;
        _lastQuotaSuccessSnapshot = null;
    }
}
