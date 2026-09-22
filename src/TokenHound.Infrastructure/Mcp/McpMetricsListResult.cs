using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains current metrics for every visible real provider.</summary>
public sealed record McpMetricsListResult
{
    /// <summary>Gets the UTC time when the store was read.</summary>
    [JsonPropertyName("observedAtUtc")]
    public required DateTimeOffset ObservedAtUtc { get; init; }

    /// <summary>Gets the providers with available snapshots.</summary>
    [JsonPropertyName("providers")]
    public required IReadOnlyList<McpProviderMetrics> Providers { get; init; }
}
