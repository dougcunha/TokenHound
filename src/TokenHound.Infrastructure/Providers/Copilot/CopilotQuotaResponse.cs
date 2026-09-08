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

    /// <summary>
    /// Gets the user login reported by the internal quota response, if present.
    /// </summary>
    [JsonPropertyName("login")]
    public string? Login { get; init; }

    /// <summary>
    /// Gets the Copilot plan identifier hint reported by the internal quota endpoint, if present.
    /// </summary>
    [JsonPropertyName("copilot_plan")]
    public string? CopilotPlan { get; init; }

    /// <summary>
    /// Gets the access type SKU reported by the internal quota endpoint, if present.
    /// </summary>
    [JsonPropertyName("access_type_sku")]
    public string? AccessTypeSku { get; init; }

    /// <summary>
    /// Gets the list of candidate organization logins reported by the internal quota endpoint, if present.
    /// </summary>
    [JsonPropertyName("organization_login_list")]
    public List<string>? OrganizationLoginList { get; init; }
}
