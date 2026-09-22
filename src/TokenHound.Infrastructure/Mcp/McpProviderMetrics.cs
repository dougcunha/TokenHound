using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Mcp;

/// <summary>Contains allowlisted usage and status data for one provider.</summary>
public sealed record McpProviderMetrics
{
    /// <summary>Gets the stable provider identifier.</summary>
    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    /// <summary>Gets the current provider status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Gets the origin and confidence of the reported metrics.</summary>
    [JsonPropertyName("fidelity")]
    public required string Fidelity { get; init; }

    /// <summary>Gets the timestamp attached to the current status-bearing snapshot.</summary>
    [JsonPropertyName("snapshotFetchedAtUtc")]
    public required DateTimeOffset SnapshotFetchedAtUtc { get; init; }

    /// <summary>Gets the time of the last successful snapshot, if retained.</summary>
    [JsonPropertyName("lastSuccessfulAtUtc")]
    public required DateTimeOffset? LastSuccessfulAtUtc { get; init; }

    /// <summary>Gets the provider's reported quota windows.</summary>
    [JsonPropertyName("limitWindows")]
    public required IReadOnlyList<McpLimitWindowMetrics> LimitWindows { get; init; }

    /// <summary>Gets an active usage block when one is reported.</summary>
    [JsonPropertyName("activeBlock")]
    public McpBlockMetrics? ActiveBlock { get; init; }

    /// <summary>Gets separate Copilot billing telemetry when available.</summary>
    [JsonPropertyName("copilotBilling")]
    public McpCopilotBillingMetrics? CopilotBilling { get; init; }

    /// <summary>Gets Cline account telemetry when available.</summary>
    [JsonPropertyName("clineAccount")]
    public McpClineAccountMetrics? ClineAccount { get; init; }

    /// <summary>Gets locally aggregated Cline token use when available.</summary>
    [JsonPropertyName("clineLocal")]
    public McpClineLocalMetrics? ClineLocal { get; init; }
}
