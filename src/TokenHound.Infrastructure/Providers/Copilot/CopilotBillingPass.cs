using System;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Copilot;

internal sealed record CopilotBillingPass
{
    internal required CopilotBillingContext Context { get; init; }
    internal required CopilotBillingPeriod Period { get; init; }
    internal required string AccessToken { get; init; }
    internal required CopilotPassDispatchBudget Budget { get; init; }
    internal required DateTimeOffset NowUtc { get; init; }
}
