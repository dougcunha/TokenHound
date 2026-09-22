using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains allowlisted Copilot credit amounts and source times.</summary>
public sealed record McpCopilotCreditMetrics
{
    /// <summary>Gets the safe report coverage details.</summary>
    [JsonPropertyName("coverage")]
    public required McpCopilotCoverageMetrics Coverage { get; init; }

    /// <summary>Gets the unit type reported by billing.</summary>
    [JsonPropertyName("unitType")]
    public string? UnitType { get; init; }

    /// <summary>Gets gross credit use, if reported.</summary>
    [JsonPropertyName("grossUsed")]
    public decimal? GrossUsed { get; init; }

    /// <summary>Gets discounted credit use, if reported.</summary>
    [JsonPropertyName("discountedUsed")]
    public decimal? DiscountedUsed { get; init; }

    /// <summary>Gets net credit use, if reported.</summary>
    [JsonPropertyName("netUsed")]
    public decimal? NetUsed { get; init; }

    /// <summary>Gets the included credit capacity, if known.</summary>
    [JsonPropertyName("includedTotal")]
    public decimal? IncludedTotal { get; init; }

    /// <summary>Gets remaining credits, if reported.</summary>
    [JsonPropertyName("remaining")]
    public decimal? Remaining { get; init; }

    /// <summary>Gets the used fraction only when supported by billing evidence.</summary>
    [JsonPropertyName("usedFraction")]
    public decimal? UsedFraction { get; init; }

    /// <summary>Gets the billing source category.</summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>Gets the source's as-of time, if available.</summary>
    [JsonPropertyName("sourceAsOfUtc")]
    public DateTimeOffset? SourceAsOfUtc { get; init; }

    /// <summary>Gets the time when billing usage was fetched.</summary>
    [JsonPropertyName("fetchedAtUtc")]
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>Gets whether the reported amounts are estimated.</summary>
    [JsonPropertyName("isEstimated")]
    public required bool IsEstimated { get; init; }

    /// <summary>Gets the verified billing period start, if available.</summary>
    [JsonPropertyName("periodStartUtc")]
    public DateTimeOffset? PeriodStartUtc { get; init; }

    /// <summary>Gets the exclusive billing period end, if available.</summary>
    [JsonPropertyName("periodEndExclusiveUtc")]
    public DateTimeOffset? PeriodEndExclusiveUtc { get; init; }

    /// <summary>Gets the billing reset time, if available.</summary>
    [JsonPropertyName("periodResetUtc")]
    public DateTimeOffset? PeriodResetUtc { get; init; }

    /// <summary>Gets whether the period boundaries were verified.</summary>
    [JsonPropertyName("periodIsVerified")]
    public required bool PeriodIsVerified { get; init; }
}
