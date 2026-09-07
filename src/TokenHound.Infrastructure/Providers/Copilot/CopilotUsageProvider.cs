using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Adapts the Copilot lightweight quota endpoint into TokenHound snapshots.
/// </summary>
public sealed class CopilotUsageProvider : IUsageProvider, IDisposable
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
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;

    /// <summary>Initializes a provider with default read-only discovery and HTTP dependencies.</summary>
    public CopilotUsageProvider()
        : this(null, null, null)
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
    {
        _discovery = discovery ?? new CopilotCredentialDiscovery();
        _timeProvider = timeProvider ?? TimeProvider.System;

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
            return CreateNeedsAuthSnapshot();

        try
        {
            var response = await _client.GetQuotaAsync(
                credential.AccessToken,
                cancellationToken
            ).ConfigureAwait(false);

            return MapResponse(response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (CopilotApiException ex)
        {
            return MapHttpError(ex, credential);
        }
        catch (CopilotTimeoutException)
        {
            return CreateStaleSnapshot("Copilot quota request timed out.");
        }
        catch (JsonException)
        {
            return CreateStaleSnapshot("Copilot quota response schema was not recognized.");
        }
        catch (HttpRequestException)
        {
            return CreateStaleSnapshot("Copilot quota request failed.");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposeClient)
            _client.Dispose();
    }

    private Snapshot MapResponse(CopilotQuotaResponse response)
    {
        var result = CopilotQuotaParser.Parse(response);

        return result.Status switch
        {
            CopilotQuotaParseStatus.Success => CreateSuccessSnapshot(result),
            CopilotQuotaParseStatus.NoFiniteQuota => CreateUnsupportedSnapshot(),
            _ => CreateStaleSnapshot(result.ErrorDescription ?? "Copilot quota schema drifted.")
        };
    }

    private Snapshot MapHttpError(
        CopilotApiException exception,
        CopilotCredential credential)
    {
        return exception.StatusCode switch
        {
            HttpStatusCode.Unauthorized => CreateNeedsAuthSnapshot(),
            HttpStatusCode.Forbidden => CopilotCredential.IsPatShaped(credential.AccessToken)
                ? CreateNeedsAuthSnapshot(NEEDS_AUTH_MESSAGE + OVERRIDE_WARNING)
                : CreateUnsupportedSnapshot(
                    "Copilot has no usable finite quota or entitlement." + OVERRIDE_WARNING),
            HttpStatusCode.TooManyRequests => CreateRateLimitedSnapshot(exception),
            _ => CreateStaleSnapshot($"Copilot quota request returned HTTP {(int?)exception.StatusCode}.")
        };
    }

    private Snapshot CreateSuccessSnapshot(CopilotQuotaParseResult result)
    {
        var activeBlock = CreateQuotaBlock(result);

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [result.LimitWindow!],
            ActiveBlock = activeBlock
        };
    }

    private static UsageBlock? CreateQuotaBlock(CopilotQuotaParseResult result)
    {
        if (result.Remaining is not <= 0 || result.ResetTimeUtc is null)
            return null;

        var overagePermitted = result.OveragePermitted == true;

        return new UsageBlock
        {
            IsBlocked = !overagePermitted,
            ResetTimeUtc = result.ResetTimeUtc,
            Reason = overagePermitted
                ? "Quota exhausted; metered overage continues."
                : "Quota exhausted until the reported reset."
        };
    }

    private Snapshot CreateRateLimitedSnapshot(CopilotApiException exception)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var deadline = RateLimitPolicy.CalculateDeadline(
            nowUtc,
            exception.RetryAfterSeconds,
            1,
            Random.Shared
        );

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = nowUtc,
            LimitWindows = [],
            ActiveBlock = new UsageBlock
            {
                IsBlocked = true,
                ResetTimeUtc = deadline,
                RetryAfterSeconds = exception.RetryAfterSeconds,
                Reason = "Copilot quota request was rate limited."
            },
            ErrorDescription = "Copilot quota is temporarily rate limited."
        };
    }

    private Snapshot CreateNeedsAuthSnapshot(string? description = null)
        => CreateSnapshot(ProviderStatus.NeedsAuth, description ?? NEEDS_AUTH_MESSAGE);

    private Snapshot CreateUnsupportedSnapshot(string? description = null)
        => CreateSnapshot(
            ProviderStatus.Unsupported,
            description ?? "Copilot has no usable finite quota or entitlement.");

    private Snapshot CreateStaleSnapshot(string description)
        => CreateSnapshot(ProviderStatus.Stale, description);

    private Snapshot CreateSnapshot(ProviderStatus status, string description)
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = status,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = _timeProvider.GetUtcNow(),
            LimitWindows = [],
            ErrorDescription = description
        };
}
