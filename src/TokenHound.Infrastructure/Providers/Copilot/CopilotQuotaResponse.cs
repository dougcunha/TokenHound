using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents the top-level response from the Copilot internal quota endpoint.
/// </summary>
public sealed record CopilotQuotaResponse
{
    /// <summary>
    /// Gets the top-level UTC reset instant reported by Copilot.
    /// </summary>
    [JsonPropertyName("quota_reset_date_utc")]
    public string? QuotaResetDateUtc { get; init; }

    /// <summary>
    /// Gets the open map of quota categories returned by Copilot.
    /// </summary>
    [JsonPropertyName("quota_snapshots")]
    public Dictionary<string, CopilotQuotaSnapshotDto>? QuotaSnapshots { get; init; }
}
