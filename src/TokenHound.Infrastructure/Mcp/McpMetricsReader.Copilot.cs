using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Mcp;

public sealed partial class McpMetricsReader
{
    private static McpCopilotBillingMetrics? MapCopilotBilling(CopilotBillingStatus? billing)
    {

        return billing is null ? null : new McpCopilotBillingMetrics
        {
            State = FormatEnum(billing.State),
            Reason = FormatEnum(billing.Reason),
            AttemptedAtUtc = billing.AttemptedAtUtc,
            NextRequestAtUtc = billing.NextRequestAtUtc,
            Usage = MapCopilotCredits(billing.Usage)
        };
    }

    private static McpCopilotCreditMetrics? MapCopilotCredits(CopilotCreditUsage? usage)
    {

        return usage is null ? null : new McpCopilotCreditMetrics
        {
            Coverage = MapCoverage(usage.Coverage),
            UnitType = usage.UnitType,
            GrossUsed = usage.GrossUsed,
            DiscountedUsed = usage.DiscountedUsed,
            NetUsed = usage.NetUsed,
            IncludedTotal = usage.IncludedTotal,
            Remaining = usage.Remaining,
            UsedFraction = usage.UsedFraction,
            Source = FormatEnum(usage.Source),
            SourceAsOfUtc = usage.SourceAsOfUtc,
            FetchedAtUtc = usage.FetchedAtUtc,
            IsEstimated = usage.IsEstimated,
            PeriodStartUtc = usage.Period.StartUtc,
            PeriodEndExclusiveUtc = usage.Period.EndExclusiveUtc,
            PeriodResetUtc = usage.Period.ResetUtc,
            PeriodIsVerified = usage.Period.IsVerified
        };
    }

    private static McpCopilotCoverageMetrics MapCoverage(CopilotReportCoverage coverage)
    {

        return new McpCopilotCoverageMetrics
        {
            StartDay = coverage.StartDay,
            EndDay = coverage.EndDay,
            IsComplete = coverage.IsComplete,
            MissingDays = [.. coverage.MissingDays],
            HasMissingPartitions = coverage.HasMissingPartitions,
            HasInvalidRows = coverage.HasInvalidRows
        };
    }
}
