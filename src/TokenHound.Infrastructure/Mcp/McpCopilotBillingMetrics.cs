using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains the independent Copilot billing state.</summary>
public sealed record McpCopilotBillingMetrics
{
    /// <summary>Gets the billing reading's freshness state.</summary>
    [JsonPropertyName("state")]
    public required string State { get; init; }

    /// <summary>Gets the controlled reason category.</summary>
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Gets the time of the most recent billing attempt.</summary>
    [JsonPropertyName("attemptedAtUtc")]
    public required DateTimeOffset AttemptedAtUtc { get; init; }

    /// <summary>Gets the next permitted billing request time, if any.</summary>
    [JsonPropertyName("nextRequestAtUtc")]
    public DateTimeOffset? NextRequestAtUtc { get; init; }

    /// <summary>Gets the retained or current credit usage, if available.</summary>
    [JsonPropertyName("usage")]
    public McpCopilotCreditMetrics? Usage { get; init; }
}
