using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains one quota window without inferred limits.</summary>
public sealed record McpLimitWindowMetrics
{
    /// <summary>Gets the provider's window name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Gets the provider's group name when reported.</summary>
    [JsonPropertyName("groupName")]
    public string? GroupName { get; init; }

    /// <summary>Gets the used fraction when a total is known.</summary>
    [JsonPropertyName("usedFraction")]
    public double? UsedFraction { get; init; }

    /// <summary>Gets the units consumed in the window when the provider reports a usage count; never a remaining count.</summary>
    [JsonPropertyName("usedUnits")]
    public long? UsedUnits { get; init; }

    /// <summary>Gets the remaining whole units when reported.</summary>
    [JsonPropertyName("remainingUnits")]
    public long? RemainingUnits { get; init; }

    /// <summary>Gets the exact remaining value when reported.</summary>
    [JsonPropertyName("remainingValue")]
    public double? RemainingValue { get; init; }

    /// <summary>Gets the reported total capacity, if any.</summary>
    [JsonPropertyName("totalUnits")]
    public long? TotalUnits { get; init; }

    /// <summary>Gets the UTC reset time when reported.</summary>
    [JsonPropertyName("resetTimeUtc")]
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>Gets the window period in seconds when known.</summary>
    [JsonPropertyName("periodSeconds")]
    public double? PeriodSeconds { get; init; }
}
