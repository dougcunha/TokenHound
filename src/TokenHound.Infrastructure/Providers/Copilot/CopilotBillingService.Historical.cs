using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotBillingService
{
    private async Task<CopilotBillingStatus> ProcessHistoricalReportsAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        string accessToken,
        CopilotPassDispatchBudget budget,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        var candidateDays = GetCandidateDaysForPeriod(period, nowUtc);
        var existingSummaries = _archive.LoadCopilotDailySummaries(context, period);
        var completedDays = existingSummaries
            .Where(static s => !s.HasMissingPartitions && !s.HasInvalidRows)
            .Select(static s => s.Day)
            .ToHashSet();

        var missingDays = candidateDays.Where(d => !completedDays.Contains(d)).OrderBy(static d => d).ToList();
        var daysToProcess = missingDays.Count > 0
            ? missingDays
            : GetRotatingCompletedDays(existingSummaries);

        CopilotBillingReason? errorReason = null;

        foreach (var day in daysToProcess)
        {
            if (!budget.CanDispatch)
                break;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await ProcessSingleDayReportAsync(
                    context,
                    period,
                    day,
                    accessToken,
                    budget,
                    nowUtc,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (RateLimitBlockedException)
            {
                return CreateRateLimitedStatus(cachedUsage, nowUtc);
            }
            catch (CopilotApiException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return CreateRateLimitedStatus(cachedUsage, nowUtc);
            }
            catch (CopilotApiException ex) when (ex.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
            {
                errorReason = CopilotBillingReason.AccessDenied;
                break;
            }
            catch (SecurityException)
            {
                errorReason = CopilotBillingReason.InvalidData;
            }
            catch (Exception)
            {
                errorReason = CopilotBillingReason.NetworkFailure;
            }
        }

        var allSummaries = _archive.LoadCopilotDailySummaries(context, period);

        if (allSummaries.Count > 0)
        {
            var usage = BuildHistoricalUsage(context, period, allSummaries, candidateDays, nowUtc);

            return new CopilotBillingStatus
            {
                State = CopilotBillingState.Available,
                Reason = CopilotBillingReason.None,
                Usage = usage,
                AttemptedAtUtc = nowUtc,
                NextRequestAtUtc = null
            };
        }

        var finalReason = errorReason ?? CopilotBillingReason.ReportUnavailable;

        return CreateStatus(finalReason, cachedUsage, nowUtc);
    }

    private async Task ProcessSingleDayReportAsync(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        DateOnly day,
        string accessToken,
        CopilotPassDispatchBudget budget,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {

        var inProgress = GetOrCreateInProgressReport(day);

        if (inProgress.DownloadLinks.Count == 0)
        {
            var manifest = await _metricsClient.GetMetricsManifestAsync(
                context.Scope,
                context.OwnerId!,
                day,
                accessToken,
                budget,
                cancellationToken
            ).ConfigureAwait(false);

            if (manifest is null || manifest.DownloadLinks.Count == 0)
                return;

            inProgress.DownloadLinks = manifest.DownloadLinks;
        }

        while (inProgress.NextPartitionIndex < inProgress.DownloadLinks.Count)
        {
            if (!budget.CanDispatch)
                return;

            var link = inProgress.DownloadLinks[inProgress.NextPartitionIndex];
            using var stream = await _metricsClient.DownloadReportAsync(link, budget, cancellationToken).ConfigureAwait(false);
            var result = await CopilotMetricsReportParser.ParseAsync(
                stream,
                day,
                context.PrincipalId,
                context.OwnerId,
                context.Scope,
                cancellationToken
            ).ConfigureAwait(false);

            inProgress.DownloadedPartitions.Add(result);
            inProgress.NextPartitionIndex++;
        }

        var merged = CopilotMetricsReportParser.Merge(day, inProgress.DownloadedPartitions, context.PrincipalId);
        _inProgressDayReports.Remove(day);

        if (merged.IsDayInvalid || (merged.UserCount == 0 && merged.HasInvalidRows))
            return;

        var summary = new CopilotDailyUsageSummary
        {
            Day = day,
            AiCreditsUsed = merged.TotalCreditsUsed,
            UserCount = merged.UserCount,
            HasMissingPartitions = false,
            HasInvalidRows = merged.HasInvalidRows,
            FetchedAtUtc = nowUtc
        };

        await _archive.SaveCopilotDailySummaryAsync(
            context,
            period,
            summary,
            null,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private InProgressDayReport GetOrCreateInProgressReport(DateOnly day)
    {

        if (!_inProgressDayReports.TryGetValue(day, out var report))
        {
            report = new InProgressDayReport { Day = day };
            _inProgressDayReports[day] = report;
        }

        return report;
    }

    private bool HasPendingReportPartitions()
        => _inProgressDayReports.Values.Any(static r => r.DownloadLinks.Count > 0 && r.NextPartitionIndex < r.DownloadLinks.Count);

    private static IReadOnlyList<DateOnly> GetRotatingCompletedDays(
        IReadOnlyList<CopilotDailyUsageSummary> summaries)
        => summaries
            .OrderBy(static s => s.FetchedAtUtc)
            .Select(static s => s.Day)
            .ToList();

    private CopilotCreditUsage? LoadHistoricalUsageFromArchive(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        DateTimeOffset nowUtc)
    {

        var summaries = _archive.LoadCopilotDailySummaries(context, period);

        if (summaries.Count == 0)
            return null;

        var candidateDays = GetCandidateDaysForPeriod(period, nowUtc);

        return BuildHistoricalUsage(context, period, summaries, candidateDays, nowUtc);
    }

    private static bool CanUseHistoricalReports(CopilotBillingScope scope)
        => scope is CopilotBillingScope.Organization or CopilotBillingScope.Enterprise;

    private sealed class InProgressDayReport
    {
        public required DateOnly Day { get; init; }
        public IReadOnlyList<string> DownloadLinks { get; set; } = [];
        public List<CopilotMetricsReportParseResult> DownloadedPartitions { get; } = [];
        public int NextPartitionIndex { get; set; }
    }
}
