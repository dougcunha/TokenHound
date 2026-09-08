using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents the response from the GitHub billing AI credit usage endpoint.
/// </summary>
public sealed record CopilotBillingResponse
{
    /// <summary>
    /// Gets the organization owner login, when the response is for an organization.
    /// </summary>
    [JsonPropertyName("organization")]
    public string? Organization { get; init; }

    /// <summary>
    /// Gets the user owner login, when the response is for a personal user.
    /// </summary>
    [JsonPropertyName("user")]
    public string? User { get; init; }

    /// <summary>
    /// Gets the enterprise owner name, when the response is for an enterprise.
    /// </summary>
    [JsonPropertyName("enterprise")]
    public string? Enterprise { get; init; }

    /// <summary>
    /// Gets the time period reported by the billing response.
    /// </summary>
    [JsonPropertyName("timePeriod")]
    public CopilotBillingTimePeriodDto? TimePeriod { get; init; }

    /// <summary>
    /// Gets the list of usage items reported for the period.
    /// </summary>
    [JsonPropertyName("usageItems")]
    public IReadOnlyList<CopilotBillingUsageItem>? UsageItems { get; init; }

    /// <summary>
    /// Represents the reported billing time period.
    /// </summary>
    public sealed record CopilotBillingTimePeriodDto
    {
        /// <summary>
        /// Gets the billing year.
        /// </summary>
        [JsonPropertyName("year")]
        public int? Year { get; init; }

        /// <summary>
        /// Gets the billing month.
        /// </summary>
        [JsonPropertyName("month")]
        public int? Month { get; init; }
    }
}
