using System.Text.Json.Serialization;

namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Persisted preference for the translucent (Acrylic) HUD background.
/// </summary>
public sealed record HudBackdropSettings
{
    /// <summary>
    /// Gets the persisted preference, or <see langword="null"/> to use the default (enabled).
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// Gets a value indicating whether the translucent background is requested; a missing value means enabled.
    /// </summary>
    [JsonIgnore]
    public bool IsEnabled
        => Enabled ?? true;
}
