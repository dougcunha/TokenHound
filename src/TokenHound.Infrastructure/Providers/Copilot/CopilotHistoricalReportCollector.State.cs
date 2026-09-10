using System;
using System.Collections.Generic;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

internal sealed partial class CopilotHistoricalReportCollector
{
    private sealed class InProgressDayReport
    {
        internal IReadOnlyList<string> DownloadLinks { get; set; } = [];
        internal List<CopilotMetricsReportParseResult> DownloadedPartitions { get; } = [];
        internal int NextPartitionIndex { get; set; }
    }

    private readonly record struct BillingContextKey(
        string PrincipalId,
        CopilotBillingScope Scope,
        string OwnerId);

    private readonly record struct InProgressReportKey(
        BillingContextKey Context,
        DateOnly Day);
}
