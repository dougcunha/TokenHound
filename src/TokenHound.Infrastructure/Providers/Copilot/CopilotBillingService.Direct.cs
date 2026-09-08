using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotBillingService
{
    private async Task<CopilotBillingStatus> TryFetchDirectBillingAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        string accessToken,
        CopilotPassDispatchBudget budget,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        try
        {
            var response = await _client.GetBillingUsageAsync(
                context.Scope,
                context.OwnerId!,
                period.RequestedYear,
                period.RequestedMonth,
                accessToken,
                cancellationToken
            ).ConfigureAwait(false);

            return await ProcessResponseAsync(
                context,
                period,
                response,
                cachedUsage,
                nowUtc,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CreateStatus(CopilotBillingReason.NetworkFailure, cachedUsage, nowUtc);
        }
        catch (RateLimitBlockedException)
        {
            return CreateRateLimitedStatus(cachedUsage, nowUtc);
        }
        catch (CopilotApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return CreateRateLimitedStatus(cachedUsage, nowUtc);
        }
        catch (CopilotApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            if (CanUseHistoricalReports(context.Scope))
            {
                _directBillingPermanentlyUnavailable = true;

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

            var reason = ex.StatusCode == HttpStatusCode.NotFound
                ? CopilotBillingReason.ReportUnavailable
                : CopilotBillingReason.AccessDenied;

            return CreateStatus(reason, cachedUsage, nowUtc);
        }
        catch (Exception ex) when (ex is TimeoutException or CopilotTimeoutException or HttpRequestException)
        {
            return CreateStatus(CopilotBillingReason.NetworkFailure, cachedUsage, nowUtc);
        }
        catch (JsonException)
        {
            return CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, nowUtc);
        }
    }

    private async Task<CopilotBillingStatus> ProcessResponseAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        CopilotBillingResponse response,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        if (response.TimePeriod is null
            || response.TimePeriod.Year != period.RequestedYear
            || response.TimePeriod.Month != period.RequestedMonth)
            return CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, nowUtc);

        var items = ExtractCreditItems(response.UsageItems);
        var coverage = CreateCoverage(period);
        var filter = new CopilotCreditFilter
        {
            Product = "Copilot",
            Sku = "Copilot AI Credits",
            UnitType = "ai-credits"
        };

        var request = new CopilotCreditAggregationRequest
        {
            Context = context,
            Period = period,
            Coverage = coverage,
            Source = CopilotCreditSource.BillingApi,
            SourceAsOfUtc = null,
            FetchedAtUtc = nowUtc,
            IsEstimated = false,
            Filter = filter,
            Items = items,
            Allowance = null
        };

        var result = CopilotCreditPolicy.Evaluate(request);

        if (result.Outcome != CopilotCreditPolicyOutcome.Valid)
            return CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, nowUtc);

        return await PersistAndCreateAvailableStatusAsync(result.Usage, nowUtc, cancellationToken).ConfigureAwait(false);
    }

    private async Task<CopilotBillingStatus> PersistAndCreateAvailableStatusAsync(
        CopilotCreditUsage usage,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        try
        {
            await _archive.SaveCopilotBillingAsync(usage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return new CopilotBillingStatus
            {
                State = CopilotBillingState.Available,
                Reason = CopilotBillingReason.PersistenceFailure,
                Usage = usage,
                AttemptedAtUtc = nowUtc,
                NextRequestAtUtc = null
            };
        }

        return new CopilotBillingStatus
        {
            State = CopilotBillingState.Available,
            Reason = CopilotBillingReason.None,
            Usage = usage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = null
        };
    }
}
