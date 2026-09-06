using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Root model representing the usage summary telemetry returned by Cursor's API.
/// </summary>
public sealed record CursorUsageResponse
{
    /// <summary>
    /// Gets the start timestamp of the current billing cycle.
    /// </summary>
    [JsonPropertyName("billingCycleStart")]
    public DateTimeOffset? BillingCycleStart { get; init; }

    /// <summary>
    /// Gets the end timestamp of the current billing cycle.
    /// </summary>
    [JsonPropertyName("billingCycleEnd")]
    public DateTimeOffset? BillingCycleEnd { get; init; }

    /// <summary>
    /// Gets the user subscription membership type (e.g., free, pro).
    /// </summary>
    [JsonPropertyName("membershipType")]
    public string? MembershipType { get; init; }

    /// <summary>
    /// Gets a value indicating whether the user has an unlimited quota plan.
    /// </summary>
    [JsonPropertyName("isUnlimited")]
    public bool? IsUnlimited { get; init; }

    /// <summary>
    /// Gets the individual usage details.
    /// </summary>
    [JsonPropertyName("individualUsage")]
    public CursorIndividualUsage? IndividualUsage { get; init; }
}

/// <summary>
/// Represents individual usage breakdowns for plan and on-demand quotas.
/// </summary>
public sealed record CursorIndividualUsage
{
    /// <summary>
    /// Gets the primary plan allowance metrics.
    /// </summary>
    [JsonPropertyName("plan")]
    public CursorPlanUsage? Plan { get; init; }

    /// <summary>
    /// Gets on-demand usage metrics.
    /// </summary>
    [JsonPropertyName("onDemand")]
    public CursorOnDemandUsage? OnDemand { get; init; }
}

/// <summary>
/// Represents plan allowance usage percentages and limits.
/// </summary>
public sealed record CursorPlanUsage
{
    /// <summary>
    /// Gets a value indicating whether plan usage is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the total percentage used of the plan allowance (0.0 to 100.0).
    /// </summary>
    [JsonPropertyName("totalPercentUsed")]
    public double? TotalPercentUsed { get; init; }

    /// <summary>
    /// Gets the percentage used of API credits (0.0 to 100.0).
    /// </summary>
    [JsonPropertyName("apiPercentUsed")]
    public double? ApiPercentUsed { get; init; }

    /// <summary>
    /// Gets the raw consumed units.
    /// </summary>
    [JsonPropertyName("used")]
    public double? Used { get; init; }

    /// <summary>
    /// Gets the configured limit units, if published.
    /// </summary>
    [JsonPropertyName("limit")]
    public double? Limit { get; init; }
}

/// <summary>
/// Represents on-demand usage limits and consumption.
/// </summary>
public sealed record CursorOnDemandUsage
{
    /// <summary>
    /// Gets a value indicating whether on-demand usage is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the consumed on-demand units.
    /// </summary>
    [JsonPropertyName("used")]
    public double? Used { get; init; }

    /// <summary>
    /// Gets the on-demand limit ceiling, if configured.
    /// </summary>
    [JsonPropertyName("limit")]
    public double? Limit { get; init; }
}
