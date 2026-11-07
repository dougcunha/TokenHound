using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.HudFeed;

/// <summary>Contains one HUD ring and its hover card rows.</summary>
public sealed record HudFeedProvider
{
    /// <summary>Gets the stable provider identifier, such as claude or claude-work.</summary>
    [JsonPropertyName("providerId")]
    public required string ProviderId { get; init; }

    /// <summary>Gets the display name the HUD shows.</summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>Gets the provider family that groups profiles, such as Claude.</summary>
    [JsonPropertyName("familyName")]
    public required string FamilyName { get; init; }

    /// <summary>Gets the short text badge of the ring.</summary>
    [JsonPropertyName("badge")]
    public required string Badge { get; init; }

    /// <summary>Gets the provider status in camelCase, such as ok, stale, or needsAuth.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Gets the status guidance the HUD shows, or null when the status is normal.</summary>
    [JsonPropertyName("statusMessage")]
    public string? StatusMessage { get; init; }

    /// <summary>Gets a value indicating whether an agent session of the provider is busy.</summary>
    [JsonPropertyName("isBusy")]
    public required bool IsBusy { get; init; }

    /// <summary>Gets the active session text, such as "PID 1234 • Active", or null when no session exists.</summary>
    [JsonPropertyName("activeSessionText")]
    public string? ActiveSessionText { get; init; }

    /// <summary>Gets the used fraction of the session window that the ring draws, or null when unmeasured.</summary>
    [JsonPropertyName("sessionUsedFraction")]
    public double? SessionUsedFraction { get; init; }

    /// <summary>Gets the HUD text under the session value, or null when unknown.</summary>
    [JsonPropertyName("sessionResetText")]
    public string? SessionResetText { get; init; }

    /// <summary>Gets the used fraction of the weekly window, or null when unmeasured.</summary>
    [JsonPropertyName("weeklyUsedFraction")]
    public double? WeeklyUsedFraction { get; init; }

    /// <summary>Gets the HUD reset text of the weekly window, or null when unknown.</summary>
    [JsonPropertyName("weeklyResetText")]
    public string? WeeklyResetText { get; init; }

    /// <summary>Gets the hover card rows in HUD order.</summary>
    [JsonPropertyName("rows")]
    public required IReadOnlyList<HudFeedRow> Rows { get; init; }
}
