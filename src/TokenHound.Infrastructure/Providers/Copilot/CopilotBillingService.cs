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
    private readonly UsageArchive _archive;
    private readonly CopilotRequestGate _gate;
    private readonly TimeProvider _timeProvider;
    private readonly bool _disposeClient;
    private readonly bool _disposeMetricsClient;
    private readonly Dictionary<DateOnly, InProgressDayReport> _inProgressDayReports = [];
    private bool _directBillingPermanentlyUnavailable;

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

        if (metricsClient is null)
        {
            _metricsClient = new CopilotMetricsClient(
                manifestHttpClient: client.HttpClient,
                downloadHttpClient: client.HttpClient,
                gate: _gate
            );
            _disposeMetricsClient = true;
        }
        else
        {
            _metricsClient = metricsClient;
            _disposeMetricsClient = false;
        }
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
        using var passCts = new CancellationTokenSource(DEFAULT_PASS_TIMEOUT);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, passCts.Token);

        try
        {
            var context = await _resolver.ResolveContextAsync(
                accessToken,
                quotaResponse,
                linkedCts.Token
            ).ConfigureAwait(false);

            if (context.Scope == CopilotBillingScope.Unknown)
                return HandleUnresolvedScope(context, nowUtc);

            return await FetchAndEvaluateBillingAsync(
                context,
                accessToken,
                nowUtc,
                linkedCts.Token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (RateLimitBlockedException)
        {
            return CreateRateLimitedStatus(null, nowUtc);
        }
        catch (Exception)
        {
            return CreateStatus(CopilotBillingReason.NetworkFailure, null, nowUtc);
        }
    }

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

        var period = CreateBillingPeriod(nowUtc);
        var cachedUsage = _archive.LoadCopilotBilling(context, period)
            ?? LoadHistoricalUsageFromArchive(context, period, nowUtc);

        if (!_gate.CanDispatch)
            return CreateRateLimitedStatus(cachedUsage, nowUtc);

        var budget = new CopilotPassDispatchBudget(MAX_PASS_DISPATCHES);

        if (HasPendingReportPartitions() && CanUseHistoricalReports(context.Scope))
        {
            return await ProcessHistoricalReportsAsync(
                context,
                period,
                accessToken,
                budget,
                cachedUsage,
                nowUtc,
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (!_directBillingPermanentlyUnavailable && budget.TryAcquire())
        {
            return await TryFetchDirectBillingAsync(
                context,
                period,
                accessToken,
                budget,
                cachedUsage,
                nowUtc,
                cancellationToken
            ).ConfigureAwait(false);
        }

        if (CanUseHistoricalReports(context.Scope))
        {
            return await ProcessHistoricalReportsAsync(
                context,
                period,
                accessToken,
                budget,
                cachedUsage,
                nowUtc,
                cancellationToken
            ).ConfigureAwait(false);
        }

        return CreateStatus(CopilotBillingReason.ReportUnavailable, cachedUsage, nowUtc);
    }
}
