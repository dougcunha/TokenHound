using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents an individual limit window containing utilization and reset timestamp.
/// </summary>
public sealed record ClaudeWindowDto
{
    /// <summary>
    /// Gets the raw utilization field, which may be absent or malformed.
    /// </summary>
    [JsonPropertyName("utilization")]
    public JsonElement UtilizationValue { get; init; }

    /// <summary>
    /// Gets the raw reset timestamp field, if supplied.
    /// </summary>
    [JsonPropertyName("resets_at")]
    public JsonElement ResetValue { get; init; }

    /// <summary>
    /// Gets a valid reported utilization percentage, or null when absent or invalid.
    /// </summary>
    [JsonIgnore]
    public double? Utilization
        => ClaudeQuotaWindowMapper.ToPercent(UtilizationValue);

    /// <summary>
    /// Gets the valid reset timestamp in UTC, if one was supplied.
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset? ResetsAt
        => ClaudeQuotaWindowMapper.ToReset(ResetValue);
}
