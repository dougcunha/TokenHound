using System;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotBillingService
{
    private CopilotBillingStatus CreateStatus(
        CopilotBillingReason reason,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc)
        => new()
        {
            State = cachedUsage is not null ? CopilotBillingState.Stale : CopilotBillingState.Unavailable,
            Reason = reason,
            Usage = cachedUsage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = null
        };

    private CopilotBillingStatus CreateRateLimitedStatus(
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc)
        => new()
        {
            State = cachedUsage is not null ? CopilotBillingState.Stale : CopilotBillingState.Unavailable,
            Reason = CopilotBillingReason.RateLimited,
            Usage = cachedUsage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = _gate.ActiveDeadlineUtc
        };

    private CopilotBillingStatus HandleUnresolvedScope(
        CopilotBillingContext context,
        DateTimeOffset nowUtc)
    {

        var reason = string.Equals(context.EvidenceKey, "ambiguous", StringComparison.OrdinalIgnoreCase)
            ? CopilotBillingReason.AmbiguousScope
            : CopilotBillingReason.UnknownScope;

        return new CopilotBillingStatus
        {
            State = CopilotBillingState.Unavailable,
            Reason = reason,
            Usage = null,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = _gate.ActiveDeadlineUtc
        };
    }

    private CopilotBillingStatus CreateHistoricalStatus(
        CopilotHistoricalReportResult result,
        CopilotCreditUsage? cachedUsage,
        DateTimeOffset nowUtc)
    {

        if (result.Reason == CopilotBillingReason.RateLimited)
            return CreateRateLimitedStatus(cachedUsage, nowUtc);

        if (result.Reason != CopilotBillingReason.None || result.Usage is null)
            return CreateStatus(result.Reason, cachedUsage, nowUtc);

        return new CopilotBillingStatus
        {
            State = CopilotBillingState.Available,
            Reason = CopilotBillingReason.None,
            Usage = result.Usage,
            AttemptedAtUtc = nowUtc,
            NextRequestAtUtc = null
        };
    }
}
