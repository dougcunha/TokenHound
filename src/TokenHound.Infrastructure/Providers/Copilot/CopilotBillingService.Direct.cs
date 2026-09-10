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
        CopilotBillingPass pass,
        CopilotCreditUsage? cachedUsage,
        CancellationToken cancellationToken)
    {

        try
        {
            var response = await FetchDirectBillingResponseAsync(pass, cancellationToken).ConfigureAwait(false);

            return await ProcessResponseAsync(
                pass,
                response,
                cachedUsage,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsExpectedDirectFailure(ex, cancellationToken))
        {
            return await HandleDirectFailureAsync(
                pass,
                cachedUsage,
                cancellationToken,
                ex
            ).ConfigureAwait(false);
        }
    }

    private Task<CopilotBillingResponse> FetchDirectBillingResponseAsync(
        CopilotBillingPass pass,
        CancellationToken cancellationToken)
        => _client.GetBillingUsageAsync(
            pass.Context.Scope,
            pass.Context.OwnerId!,
            pass.Period.RequestedYear,
            pass.Period.RequestedMonth,
            pass.AccessToken,
            cancellationToken
        );

    private static bool IsExpectedDirectFailure(Exception exception, CancellationToken cancellationToken)
        => exception switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            RateLimitBlockedException => true,
            CopilotApiException => true,
            TimeoutException or HttpRequestException or JsonException => true,
            _ => false
        };

    private Task<CopilotBillingStatus> HandleDirectFailureAsync(
        CopilotBillingPass pass,
        CopilotCreditUsage? cachedUsage,
        CancellationToken cancellationToken,
        Exception exception)
        => exception switch
        {
            RateLimitBlockedException or CopilotApiException { StatusCode: HttpStatusCode.TooManyRequests }
                => Task.FromResult(CreateRateLimitedStatus(cachedUsage, pass.NowUtc)),
            CopilotApiException api when api.StatusCode is
                HttpStatusCode.NotFound
                or HttpStatusCode.Forbidden
                or HttpStatusCode.Unauthorized => HandleDirectApiFailureAsync(
                pass,
                cachedUsage,
                api,
                cancellationToken
            ),
            CopilotApiException
                => Task.FromResult(CreateStatus(CopilotBillingReason.NetworkFailure, cachedUsage, pass.NowUtc)),
            OperationCanceledException or TimeoutException or HttpRequestException
                => Task.FromResult(CreateStatus(CopilotBillingReason.NetworkFailure, cachedUsage, pass.NowUtc)),
            JsonException
                => Task.FromResult(CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, pass.NowUtc)),
            _ => throw new ArgumentOutOfRangeException(nameof(exception), exception, null)
        };

    private async Task<CopilotBillingStatus> HandleDirectApiFailureAsync(
        CopilotBillingPass pass,
        CopilotCreditUsage? cachedUsage,
        CopilotApiException exception,
        CancellationToken cancellationToken)
    {

        if (CanUseHistoricalReports(pass.Context.Scope))
        {
            _directBillingUnavailableContexts.Add(CreateContextKey(pass.Context));

            return await CollectHistoricalStatusAsync(
                pass,
                cachedUsage,
                cancellationToken
            ).ConfigureAwait(false);
        }

        var reason = exception.StatusCode == HttpStatusCode.NotFound
            ? CopilotBillingReason.ReportUnavailable
            : CopilotBillingReason.AccessDenied;

        return CreateStatus(reason, cachedUsage, pass.NowUtc);
    }

    private async Task<CopilotBillingStatus> ProcessResponseAsync(
        CopilotBillingPass pass,
        CopilotBillingResponse response,
        CopilotCreditUsage? cachedUsage,
        CancellationToken cancellationToken)
    {

        if (response.TimePeriod is null
            || response.TimePeriod.Year != pass.Period.RequestedYear
            || response.TimePeriod.Month != pass.Period.RequestedMonth)
            return CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, pass.NowUtc);

        var request = CopilotBillingMapper.CreateDirectRequest(pass, response.UsageItems);
        var result = CopilotCreditPolicy.Evaluate(request);

        if (result.Outcome != CopilotCreditPolicyOutcome.Valid)
            return CreateStatus(CopilotBillingReason.InvalidData, cachedUsage, pass.NowUtc);

        return await PersistAndCreateAvailableStatusAsync(
            result.Usage,
            pass.NowUtc,
            cancellationToken
        ).ConfigureAwait(false);
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
            return CreateAvailableStatus(usage, nowUtc, CopilotBillingReason.PersistenceFailure);
        }

        return CreateAvailableStatus(usage, nowUtc, CopilotBillingReason.None);
    }

    private static CopilotBillingStatus CreateAvailableStatus(
        CopilotCreditUsage usage,
        DateTimeOffset nowUtc,
        CopilotBillingReason reason)
        => new()
        {
            State = CopilotBillingState.Available,
            Reason = reason,
            Usage = usage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = null
        };
}
