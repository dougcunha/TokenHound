using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.HudFeed;

/// <summary>Contains one quota or credit row of the HUD hover card.</summary>
public sealed record HudFeedRow
{
    /// <summary>Gets the row identifier, unique inside its provider, such as quota:0.</summary>
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    /// <summary>Gets the row label, such as "Current session (5h)".</summary>
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    /// <summary>Gets the used fraction, or null when the provider reports no total; never inferred.</summary>
    [JsonPropertyName("usedFraction")]
    public double? UsedFraction { get; init; }

    /// <summary>Gets the main quantity text, such as "63% Used" or "~120 requests".</summary>
    [JsonPropertyName("primaryText")]
    public string? PrimaryText { get; init; }

    /// <summary>Gets the secondary quantity text, such as "1,200 of 1,500 remaining".</summary>
    [JsonPropertyName("secondaryText")]
    public string? SecondaryText { get; init; }

    /// <summary>Gets the scope text, such as a model group, or null when not reported.</summary>
    [JsonPropertyName("scopeText")]
    public string? ScopeText { get; init; }

    /// <summary>Gets the reset countdown text at write time, such as "Resets in 2h 10m".</summary>
    [JsonPropertyName("resetText")]
    public string? ResetText { get; init; }

    /// <summary>Gets the UTC reset time, so a reader can count down between writes.</summary>
    [JsonPropertyName("resetTimeUtc")]
    public DateTimeOffset? ResetTimeUtc { get; init; }

    /// <summary>Gets the source and freshness text, or null when not shown.</summary>
    [JsonPropertyName("provenanceText")]
    public string? ProvenanceText { get; init; }

    /// <summary>Gets the error or unavailable text, or null when the row is normal.</summary>
    [JsonPropertyName("errorText")]
    public string? ErrorText { get; init; }
}
