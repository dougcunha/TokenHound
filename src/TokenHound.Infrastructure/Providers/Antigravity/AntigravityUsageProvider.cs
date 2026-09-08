using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Implements the usage provider for Google Antigravity / Gemini with multi-tier fallback.
/// </summary>
public sealed class AntigravityUsageProvider : IUsageProvider, IDisposable
{
    private const string PROVIDER_ID = "gemini";

    private readonly AntigravityEndpointDiscovery _discovery;
    private readonly AntigravityLanguageServerClient _client;
    private readonly AntigravityCloudCodeClient _cloudCodeClient;
    private readonly AntigravityTranscriptReader _transcriptReader;
    private readonly bool _disposeClient;
    private readonly bool _disposeCloudCodeClient;

    /// <inheritdoc />
    public string ProviderId => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="AntigravityUsageProvider"/> class.
    /// </summary>
    /// <param name="discovery">Optional endpoint discovery instance.</param>
    /// <param name="client">Optional language server client.</param>
    /// <param name="transcriptReader">Optional transcript reader instance.</param>
    /// <param name="cloudCodeClient">Optional Cloud Code client instance.</param>
    public AntigravityUsageProvider(
        AntigravityEndpointDiscovery? discovery = null,
        AntigravityLanguageServerClient? client = null,
        AntigravityTranscriptReader? transcriptReader = null,
        AntigravityCloudCodeClient? cloudCodeClient = null)
    {
        _discovery = discovery ?? new AntigravityEndpointDiscovery();

        if (client is not null)
        {
            _client = client;
            _disposeClient = false;
        }
        else
        {
            _client = new AntigravityLanguageServerClient();
            _disposeClient = true;
        }

        if (cloudCodeClient is not null)
        {
            _cloudCodeClient = cloudCodeClient;
            _disposeCloudCodeClient = false;
        }
        else
        {
            _cloudCodeClient = new AntigravityCloudCodeClient();
            _disposeCloudCodeClient = true;
        }

        _transcriptReader = transcriptReader ?? new AntigravityTranscriptReader();
    }

    /// <inheritdoc />
    public async ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var officialSnapshot = await TryGetLanguageServerSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (officialSnapshot is not null)
        {
            return officialSnapshot;
        }

        var cloudCodeSnapshot = await TryGetCloudCodeSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (cloudCodeSnapshot is not null)
        {
            return cloudCodeSnapshot;
        }

        var derivedSnapshot = await TryGetTranscriptSnapshotAsync(cancellationToken).ConfigureAwait(false);

        if (derivedSnapshot is not null)
        {
            return derivedSnapshot;
        }

        return CreateNeedsAuthSnapshot();
    }

    private async ValueTask<Snapshot?> TryGetLanguageServerSnapshotAsync(CancellationToken cancellationToken)
    {
        var endpoint = _discovery.DiscoverEndpoint();

        if (endpoint is null)
        {
            return null;
        }

        var quota = await _client.RetrieveUserQuotaSummaryAsync(endpoint, cancellationToken).ConfigureAwait(false);

        if (quota?.Groups is null || quota.Groups.Count == 0)
        {
            return null;
        }

        var windows = MapQuotaGroups(quota.Groups);

        if (windows.Count == 0)
        {
            return null;
        }

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

                var period = ResolveBucketPeriod(bucket);
                var name = bucket.DisplayName ?? group.DisplayName ?? bucket.BucketId ?? "Quota Window";

                windows.Add(new LimitWindow
                {
                    Name = name,
                    GroupName = group.DisplayName,
                    Period = period,
                    TotalUnits = usedFraction.HasValue ? 100 : null,
                    UsedFraction = usedFraction,
                    RemainingUnits = remainingUnits,
                    ResetTimeUtc = bucket.ResetTime
                });
            }
        }

        return windows;
    }

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

    private async ValueTask<Snapshot?> TryGetTranscriptSnapshotAsync(CancellationToken cancellationToken)
    {
        var hasTranscripts = _transcriptReader.HasAnyTranscripts();
        var hasCredentials = await _cloudCodeClient.HasCredentialAsync(cancellationToken).ConfigureAwait(false);

        if (!hasTranscripts && !hasCredentials)
        {
            return null;
        }

        var requestsToday = await _transcriptReader.CountTodayModelRequestsAsync(cancellationToken).ConfigureAwait(false);

        var limitWindow = new LimitWindow
        {
            Name = "Requests Today",
            TotalUnits = null,
            UsedFraction = null,
            RemainingUnits = requestsToday,
            ResetTimeUtc = null
        };

        return new Snapshot
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.Ok,
            Fidelity = Fidelity.Derived,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [limitWindow],
            ActiveBlock = null,
            ErrorDescription = null
        };
    }

    private static Snapshot CreateNeedsAuthSnapshot()
        => new()
        {
            ProviderId = PROVIDER_ID,
            Status = ProviderStatus.NeedsAuth,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = [],
            ActiveBlock = null,
            ErrorDescription = "Launch Antigravity IDE or login to Gemini"
        };

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
