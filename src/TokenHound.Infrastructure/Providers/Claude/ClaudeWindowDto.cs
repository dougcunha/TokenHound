using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents an individual limit window containing utilization and reset timestamp.
/// </summary>
public sealed record ClaudeWindowDto
{
    /// <summary>
    /// Gets the utilization fraction or percentage for this limit window.
    /// </summary>
    [JsonPropertyName("utilization")]
    public double Utilization { get; init; }

    /// <summary>
    /// Gets the timestamp when this limit window resets in UTC, if available.
    /// </summary>
    [JsonPropertyName("resets_at")]
    public DateTimeOffset? ResetsAt { get; init; }
}
