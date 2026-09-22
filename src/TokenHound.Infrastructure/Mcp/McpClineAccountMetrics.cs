using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains the safe Cline account values shown by the HUD.</summary>
public sealed record McpClineAccountMetrics
{
    /// <summary>Gets the remaining credit balance, if reported.</summary>
    [JsonPropertyName("balanceCredits")]
    public double? BalanceCredits { get; init; }

    /// <summary>Gets the reported plan name, if any.</summary>
    [JsonPropertyName("planName")]
    public string? PlanName { get; init; }

    /// <summary>Gets whether the account has a Pass subscription.</summary>
    [JsonPropertyName("hasPassSubscription")]
    public required bool HasPassSubscription { get; init; }
}
