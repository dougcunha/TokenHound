using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Reports one provider's metrics or why they are unavailable.</summary>
public sealed record McpMetricsLookupResult
{
    /// <summary>Gets available, unknown, disabled, pending, or synthetic.</summary>
    [JsonPropertyName("lookupState")]
    public required string LookupState { get; init; }

    /// <summary>Gets the UTC time when the store was read.</summary>
    [JsonPropertyName("observedAtUtc")]
    public required DateTimeOffset ObservedAtUtc { get; init; }

    /// <summary>Gets the requested provider metrics when available.</summary>
    [JsonPropertyName("provider")]
    public McpProviderMetrics? Provider { get; init; }
}
