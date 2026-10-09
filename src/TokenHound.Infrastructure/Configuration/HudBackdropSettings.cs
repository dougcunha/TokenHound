using System;
using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted preference for the translucent (Acrylic) HUD background.
/// </summary>
public sealed record HudBackdropSettings
{
    /// <summary>Lowest supported transparency, in percent; the tint then covers the material completely.</summary>
    public const int MINIMUM_TRANSPARENCY = 0;

    /// <summary>Highest supported transparency, in percent.</summary>
    public const int MAXIMUM_TRANSPARENCY = 50;

    /// <summary>Default transparency, in percent; it keeps the approved 93 % tint.</summary>
    public const int DEFAULT_TRANSPARENCY = 7;

    /// <summary>
    /// Gets the persisted preference, or <see langword="null"/> to use the default (enabled).
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets the persisted transparency of the capsule tint in percent, or <see langword="null"/> to use the default.
    /// </summary>
    public int? Transparency { get; init; }

    /// <summary>
    /// Gets a value indicating whether the translucent background is requested; a missing value means enabled.
    /// </summary>
    [JsonIgnore]
    public bool IsEnabled
        => Enabled ?? true;

    /// <summary>
    /// Gets the transparency in percent, defaulted and clamped to the supported range.
    /// </summary>
    [JsonIgnore]
    public int TransparencyPercent
        => Math.Clamp(Transparency ?? DEFAULT_TRANSPARENCY, MINIMUM_TRANSPARENCY, MAXIMUM_TRANSPARENCY);
}
