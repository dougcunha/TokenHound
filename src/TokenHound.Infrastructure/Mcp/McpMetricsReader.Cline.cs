using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Mcp;

public sealed partial class McpMetricsReader
{
    private static McpClineAccountMetrics? MapClineAccount(ClineAccountUsage? account)
    {

        return account is null ? null : new McpClineAccountMetrics
        {
            BalanceCredits = account.BalanceCredits,
            PlanName = account.PlanName,
            HasPassSubscription = account.HasPassSubscription
        };
    }

    private static McpClineLocalMetrics? MapClineLocal(ClineLocalUsage? local)
    {

        return local is null ? null : new McpClineLocalMetrics
        {
            InputTokens = local.InputTokens,
            OutputTokens = local.OutputTokens,
            CacheReadTokens = local.CacheReadTokens,
            CacheWriteTokens = local.CacheWriteTokens,
            TotalTokens = local.TotalTokens,
            ModelCalls = local.ModelCalls,
            WindowStartUtc = local.WindowStartUtc,
            LastActivityUtc = local.LastActivityUtc
        };
    }
}
