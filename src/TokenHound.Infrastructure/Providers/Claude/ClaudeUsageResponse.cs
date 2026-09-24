using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Claude;

/// <summary>
/// Represents the usage telemetry response returned by the Anthropic OAuth usage API.
/// </summary>
public sealed record ClaudeUsageResponse
{
    /// <summary>
    /// Gets the five-hour rolling session limit window, if present in the response.
    /// </summary>
    [JsonPropertyName("five_hour")]
    public ClaudeWindowDto? FiveHour { get; init; }

    /// <summary>
    /// Gets the seven-day quota limit window, if present in the response.
    /// </summary>
    [JsonPropertyName("seven_day")]
    public ClaudeWindowDto? SevenDay { get; init; }

    /// <summary>
    /// Gets the optional provider limit entries in their original JSON shape.
    /// </summary>
    [JsonPropertyName("limits")]
    public JsonElement? Limits { get; init; }

    /// <summary>
    /// Gets additional top-level fields for selective weekly quota discovery.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalFields { get; init; }
}
