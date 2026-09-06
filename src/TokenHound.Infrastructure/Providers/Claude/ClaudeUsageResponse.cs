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
}
