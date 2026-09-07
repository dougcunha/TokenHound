using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Represents one open-map quota category returned by Copilot.
/// </summary>
public sealed record CopilotQuotaSnapshotDto
{
    /// <summary>Gets the provider category identifier.</summary>
    [JsonPropertyName("quota_id")]
    public string? QuotaId { get; init; }

    /// <summary>Gets whether this category is unlimited.</summary>
    [JsonPropertyName("unlimited")]
    public bool? Unlimited { get; init; }

    /// <summary>Gets whether this category has an entitlement.</summary>
    [JsonPropertyName("has_quota")]
    public bool? HasQuota { get; init; }

    /// <summary>Gets the integer entitlement for the current period.</summary>
    [JsonPropertyName("entitlement")]
    public int? Entitlement { get; init; }

    /// <summary>Gets the integer remainder for the current period.</summary>
    [JsonPropertyName("remaining")]
    public int? Remaining { get; init; }

    /// <summary>Gets the fractional remainder for the current period.</summary>
    [JsonPropertyName("quota_remaining")]
    public double? QuotaRemaining { get; init; }

    /// <summary>Gets the provider-calculated remaining percentage.</summary>
    [JsonPropertyName("percent_remaining")]
    public double? PercentRemaining { get; init; }

    /// <summary>Gets whether metered overage continues after exhaustion.</summary>
    [JsonPropertyName("overage_permitted")]
    public bool? OveragePermitted { get; init; }
}
