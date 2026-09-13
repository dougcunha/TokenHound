using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Engine;
using TokenHound.Infrastructure.Providers;

namespace TokenHound.Infrastructure.Providers.Copilot;

internal sealed partial class CopilotHistoricalReportCollector
{
    private readonly CopilotMetricsClient _metricsClient;
    private readonly UsageArchive _archive;
    private readonly Dictionary<InProgressReportKey, InProgressDayReport> _inProgressReports = [];

    internal CopilotHistoricalReportCollector(
        CopilotMetricsClient metricsClient,
        UsageArchive archive)
    {

        _metricsClient = metricsClient;
        _archive = archive;
    }

    internal bool HasPending(CopilotBillingContext context)
    {

        var contextKey = CreateContextKey(context);

        return _inProgressReports.Any(entry =>
            entry.Key.Context == contextKey
            && entry.Value.DownloadLinks.Count > 0
            && entry.Value.NextPartitionIndex < entry.Value.DownloadLinks.Count);
    }

    internal CopilotCreditUsage? LoadCachedUsage(
        CopilotBillingContext context,
        CopilotBillingPeriod period,
        DateTimeOffset nowUtc)
    {

        var summaries = _archive.LoadCopilotDailySummaries(context, period);

        if (summaries.Count == 0)
            return null;

        var candidateDays = CopilotBillingMapper.GetCandidateDays(period, nowUtc);

        return CopilotBillingMapper.BuildHistoricalUsage(context, period, summaries, candidateDays, nowUtc);
    }

    internal async Task<CopilotHistoricalReportResult> CollectAsync(
        CopilotBillingPass pass,
        CancellationToken cancellationToken)
    {

        var candidateDays = CopilotBillingMapper.GetCandidateDays(pass.Period, pass.NowUtc);
        var existingSummaries = _archive.LoadCopilotDailySummaries(pass.Context, pass.Period);
        var daysToProcess = ResolveDaysToProcess(candidateDays, existingSummaries);
        CopilotBillingReason? errorReason = null;

        foreach (var day in daysToProcess)
        {
            if (!pass.Budget.CanDispatch)
                break;

            cancellationToken.ThrowIfCancellationRequested();
            var dayError = await TryProcessDayAsync(pass, day, cancellationToken).ConfigureAwait(false);

            if (dayError is CopilotBillingReason.RateLimited or CopilotBillingReason.AccessDenied)
                return new CopilotHistoricalReportResult { Reason = dayError.Value };

            errorReason ??= dayError;
        }

        return BuildResult(pass, candidateDays, errorReason);
    }

    private async Task<CopilotBillingReason?> TryProcessDayAsync(
        CopilotBillingPass pass,
        DateOnly day,
        CancellationToken cancellationToken)
    {

        try
        {
            await ProcessDayAsync(pass, day, cancellationToken).ConfigureAwait(false);

            return null;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return MapFailure(ex);
        }
    }

    private async Task ProcessDayAsync(
        CopilotBillingPass pass,
        DateOnly day,
        CancellationToken cancellationToken)
    {

        var reportKey = new InProgressReportKey(CreateContextKey(pass.Context), day);
        var inProgress = GetOrCreateReport(reportKey);

        if (inProgress.DownloadLinks.Count == 0)
            await LoadManifestAsync(pass, day, inProgress, cancellationToken).ConfigureAwait(false);

        await DownloadPartitionsAsync(pass, day, inProgress, cancellationToken).ConfigureAwait(false);

        if (inProgress.NextPartitionIndex < inProgress.DownloadLinks.Count)
            return;

        await PersistCompletedReportAsync(pass, day, reportKey, inProgress, cancellationToken).ConfigureAwait(false);
    }

    private async Task LoadManifestAsync(
        CopilotBillingPass pass,
        DateOnly day,
        InProgressDayReport inProgress,
        CancellationToken cancellationToken)
    {

        var manifest = await _metricsClient.GetMetricsManifestAsync(
            pass.Context.Scope,
            pass.Context.OwnerId!,
            day,
            pass.AccessToken,
            pass.Budget,
            cancellationToken
        ).ConfigureAwait(false);

        if (manifest is not null)
            inProgress.DownloadLinks = manifest.DownloadLinks;
    }

    private async Task DownloadPartitionsAsync(
        CopilotBillingPass pass,
        DateOnly day,
        InProgressDayReport inProgress,
        CancellationToken cancellationToken)
    {

        while (inProgress.NextPartitionIndex < inProgress.DownloadLinks.Count && pass.Budget.CanDispatch)
        {
            var link = inProgress.DownloadLinks[inProgress.NextPartitionIndex];
            using var stream = await _metricsClient.DownloadReportAsync(
                link,
                pass.Budget,
                cancellationToken
            ).ConfigureAwait(false);
            var result = await CopilotMetricsReportParser.ParseAsync(
                stream,
                day,
                pass.Context.PrincipalId,
                pass.Context.OwnerId,
                pass.Context.Scope,
                cancellationToken
            ).ConfigureAwait(false);

            inProgress.DownloadedPartitions.Add(result);
            inProgress.NextPartitionIndex++;
        }
    }

    private async Task PersistCompletedReportAsync(
        CopilotBillingPass pass,
        DateOnly day,
        InProgressReportKey reportKey,
        InProgressDayReport inProgress,
        CancellationToken cancellationToken)
    {

        var merged = CopilotMetricsReportParser.Merge(
            day,
            inProgress.DownloadedPartitions,
            pass.Context.PrincipalId
        );
        _inProgressReports.Remove(reportKey);

        if (merged.IsDayInvalid || merged.UserCount == 0 && merged.HasInvalidRows)
            return;

        await _archive.SaveCopilotDailySummaryAsync(
            pass.Context,
            pass.Period,
            CreateSummary(day, merged, pass.NowUtc),
            null,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private CopilotHistoricalReportResult BuildResult(
        CopilotBillingPass pass,
        IReadOnlyList<DateOnly> candidateDays,
        CopilotBillingReason? errorReason)
    {

        var summaries = _archive.LoadCopilotDailySummaries(pass.Context, pass.Period);

        return summaries.Count > 0
            ? new CopilotHistoricalReportResult
            {
                Reason = CopilotBillingReason.None,
                Usage = CopilotBillingMapper.BuildHistoricalUsage(
                    pass.Context,
                    pass.Period,
                    summaries,
                    candidateDays,
                    pass.NowUtc
                )
            }
            : new CopilotHistoricalReportResult
            {
                Reason = errorReason ?? CopilotBillingReason.ReportUnavailable
            };
    }

    private static IReadOnlyList<DateOnly> ResolveDaysToProcess(
        IReadOnlyList<DateOnly> candidateDays,
        IReadOnlyList<CopilotDailyUsageSummary> summaries)
    {

        var completedDays = summaries
            .Where(static summary => !summary.HasMissingPartitions && !summary.HasInvalidRows)
            .Select(static summary => summary.Day)
            .ToHashSet();
        var missingDays = candidateDays.Where(day => !completedDays.Contains(day)).OrderBy(static day => day).ToList();

        return missingDays.Count > 0
            ? missingDays
            : summaries.OrderBy(static summary => summary.FetchedAtUtc).Select(static summary => summary.Day).ToList();
    }

    private InProgressDayReport GetOrCreateReport(InProgressReportKey key)
    {

        if (!_inProgressReports.TryGetValue(key, out var report))
        {
            report = new InProgressDayReport();
            _inProgressReports[key] = report;
        }

        return report;
    }

    private static CopilotDailyUsageSummary CreateSummary(
        DateOnly day,
        CopilotMetricsReportParseResult result,
        DateTimeOffset nowUtc)
        => new()
        {
            Day = day,
            AiCreditsUsed = result.TotalCreditsUsed,
            UserCount = result.UserCount,
            HasMissingPartitions = false,
            HasInvalidRows = result.HasInvalidRows,
            FetchedAtUtc = nowUtc
        };

    private static BillingContextKey CreateContextKey(CopilotBillingContext context)
        => new(
            context.PrincipalId.Trim().ToLowerInvariant(),
            context.Scope,
            (context.OwnerId ?? string.Empty).Trim().ToLowerInvariant()
        );

    private static CopilotBillingReason MapFailure(Exception exception)
        => exception switch
        {
            RateLimitBlockedException => CopilotBillingReason.RateLimited,
            CopilotApiException apiException => MapApiFailure(apiException),
            SecurityException => CopilotBillingReason.InvalidData,
            _ => CopilotBillingReason.NetworkFailure
        };

    private static CopilotBillingReason MapApiFailure(CopilotApiException exception)
        => SnapshotFailureMapper.Classify(exception.StatusCode, hasCredential: true, ClassifyUnauthorizedAsDenied) switch
        {
            SnapshotFailureMapper.Outcome.RateLimited => CopilotBillingReason.RateLimited,
            SnapshotFailureMapper.Outcome.AccessDenied => CopilotBillingReason.AccessDenied,
            _ => CopilotBillingReason.NetworkFailure
        };

    private static SnapshotFailureMapper.Outcome? ClassifyUnauthorizedAsDenied(HttpStatusCode? statusCode)
        => statusCode == HttpStatusCode.Unauthorized
            ? SnapshotFailureMapper.Outcome.AccessDenied
            : null;
}
