using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Retrieves, aggregates, and caches Copilot AI credit billing usage with bounded progress.
/// </summary>
public sealed partial class CopilotBillingService : IDisposable
{
    private static readonly TimeSpan DEFAULT_PASS_TIMEOUT = TimeSpan.FromSeconds(15);
    private const int MAX_PASS_DISPATCHES = 4;

    private readonly CopilotBillingClient _client;
    private readonly CopilotMetricsClient _metricsClient;
    private readonly CopilotBillingContextResolver _resolver;
    private readonly CopilotHistoricalReportCollector _historicalCollector;
    private readonly UsageArchive _archive;
    private readonly CopilotRequestGate _gate;
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;
    private readonly bool _disposeMetricsClient;
    private readonly HashSet<BillingContextKey> _directBillingUnavailableContexts = [];

    /// <summary>
    /// Initializes a billing service with required dependencies and optional clock and metrics client.
    /// </summary>
    public CopilotBillingService(
        CopilotBillingClient client,
        CopilotBillingContextResolver resolver,
        UsageArchive archive,
        CopilotRequestGate gate,
        TimeProvider? timeProvider = null,
        bool disposeClient = false,
        CopilotMetricsClient? metricsClient = null)
    {

        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(archive);
        ArgumentNullException.ThrowIfNull(gate);

        _client = client;
        _resolver = resolver;
        _archive = archive;
        _gate = gate;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _disposeClient = disposeClient;

        (_metricsClient, _disposeMetricsClient) = ResolveMetricsClient(client, metricsClient, _gate);

        _historicalCollector = new CopilotHistoricalReportCollector(_metricsClient, _archive);
    }

    private static (CopilotMetricsClient Client, bool ShouldDispose) ResolveMetricsClient(
        CopilotBillingClient client,
        CopilotMetricsClient? metricsClient,
        CopilotRequestGate gate)
    {

        if (metricsClient is not null)
            return (metricsClient, false);

        var createdClient = new CopilotMetricsClient(
            manifestHttpClient: client.HttpClient,
            downloadHttpClient: client.HttpClient,
            gate: gate
        );

        return (createdClient, true);
    }

    /// <summary>
    /// Retrieves the current Copilot billing status for the authenticated credential.
    /// </summary>
    public async Task<CopilotBillingStatus> GetBillingStatusAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        CancellationToken cancellationToken = default)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        cancellationToken.ThrowIfCancellationRequested();

        var nowUtc = _timeProvider.GetUtcNow();
        return await ExecuteBillingStatusAsync(
            accessToken,
            quotaResponse,
            nowUtc,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<CopilotBillingStatus> ExecuteBillingStatusAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        using var passCts = new CancellationTokenSource(DEFAULT_PASS_TIMEOUT);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, passCts.Token);

        try
        {

            return await ResolveAndFetchBillingAsync(
                accessToken,
                quotaResponse,
                nowUtc,
                linkedCts.Token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {

            return MapBillingFailure(ex, nowUtc);
        }
    }

    private async Task<CopilotBillingStatus> ResolveAndFetchBillingAsync(
        string accessToken,
        CopilotQuotaResponse? quotaResponse,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        var context = await _resolver.ResolveContextAsync(
            accessToken,
            quotaResponse,
            cancellationToken
        ).ConfigureAwait(false);

        if (context.Scope == CopilotBillingScope.Unknown)
            return HandleUnresolvedScope(context, nowUtc);

        return await FetchAndEvaluateBillingAsync(
            context,
            accessToken,
            nowUtc,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private CopilotBillingStatus MapBillingFailure(Exception exception, DateTimeOffset nowUtc)
        => exception is RateLimitBlockedException
            ? CreateRateLimitedStatus(null, nowUtc)
            : CreateStatus(CopilotBillingReason.NetworkFailure, null, nowUtc);

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _client.Dispose();

        if (_disposeMetricsClient)
            _metricsClient.Dispose();
    }

    private async Task<CopilotBillingStatus> FetchAndEvaluateBillingAsync(
        CopilotBillingContext context,
        string accessToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        var period = CopilotBillingMapper.CreatePeriod(nowUtc);
        var cachedUsage = _archive.LoadCopilotBilling(context, period)
            ?? _historicalCollector.LoadCachedUsage(context, period, nowUtc);

        if (!_gate.CanDispatch)
            return CreateRateLimitedStatus(cachedUsage, nowUtc);

        var pass = new CopilotBillingPass
        {
            Context = context,
            Period = period,
            AccessToken = accessToken,
            Budget = new CopilotPassDispatchBudget(MAX_PASS_DISPATCHES),
            NowUtc = nowUtc
        };

        return await ExecutePassAsync(pass, cachedUsage, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CopilotBillingStatus> ExecutePassAsync(
        CopilotBillingPass pass,
        CopilotCreditUsage? cachedUsage,
        CancellationToken cancellationToken)
    {

        var contextKey = CreateContextKey(pass.Context);

        if (_historicalCollector.HasPending(pass.Context) && CanUseHistoricalReports(pass.Context.Scope))
            return await CollectHistoricalStatusAsync(pass, cachedUsage, cancellationToken).ConfigureAwait(false);

        if (!_directBillingUnavailableContexts.Contains(contextKey) && pass.Budget.TryAcquire())
            return await TryFetchDirectBillingAsync(
                pass,
                cachedUsage,
                cancellationToken
            ).ConfigureAwait(false);

        if (CanUseHistoricalReports(pass.Context.Scope))
            return await CollectHistoricalStatusAsync(pass, cachedUsage, cancellationToken).ConfigureAwait(false);

        return CreateStatus(CopilotBillingReason.ReportUnavailable, cachedUsage, pass.NowUtc);
    }

    private async Task<CopilotBillingStatus> CollectHistoricalStatusAsync(
        CopilotBillingPass pass,
        CopilotCreditUsage? cachedUsage,
        CancellationToken cancellationToken)
    {

        var result = await _historicalCollector.CollectAsync(pass, cancellationToken).ConfigureAwait(false);

        return CreateHistoricalStatus(result, cachedUsage, pass.NowUtc);
    }

    private static bool CanUseHistoricalReports(CopilotBillingScope scope)
        => scope is CopilotBillingScope.Organization or CopilotBillingScope.Enterprise;

    private static BillingContextKey CreateContextKey(CopilotBillingContext context)
        => new(
            context.PrincipalId.Trim().ToLowerInvariant(),
            context.Scope,
            (context.OwnerId ?? string.Empty).Trim().ToLowerInvariant()
        );

    private readonly record struct BillingContextKey(
        string PrincipalId,
        CopilotBillingScope Scope,
        string OwnerId);
}
