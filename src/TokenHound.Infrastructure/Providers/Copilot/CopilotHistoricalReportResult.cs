using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

internal sealed record CopilotHistoricalReportResult
{
    internal required CopilotBillingReason Reason { get; init; }
    internal CopilotCreditUsage? Usage { get; init; }
}
