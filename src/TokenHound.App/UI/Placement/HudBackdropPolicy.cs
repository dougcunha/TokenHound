using System;

namespace TokenHound.App.UI.Placement;

/// <summary>Decides between the Acrylic material and the solid fill from availability inputs.</summary>
internal static class HudBackdropPolicy
{
    /// <summary>The first Windows 11 build documenting <c>DWMWA_USE_HOSTBACKDROPBRUSH</c>.</summary>
    public const int MINIMUM_BUILD = 22000;

    /// <summary>The reason reported when every input allows the material.</summary>
    public const string AVAILABLE = "Available";

    /// <summary>Resolves the background mode; any disabling input yields <see cref="HudBackdropMode.Solid"/>.</summary>
    /// <param name="inputs">The current availability inputs.</param>
    /// <returns>The background mode to paint.</returns>
    public static HudBackdropMode Resolve(HudBackdropInputs inputs)
        => string.Equals(Reason(inputs), AVAILABLE, StringComparison.OrdinalIgnoreCase)
            ? HudBackdropMode.Material
            : HudBackdropMode.Solid;

    /// <summary>Names the first input that disables the material, or <see cref="AVAILABLE"/>.</summary>
    /// <param name="inputs">The current availability inputs.</param>
    /// <returns>The deciding input name, for structured logging.</returns>
    public static string Reason(HudBackdropInputs inputs)
        => inputs switch
        {
            { IsEnabled: false } => nameof(HudBackdropInputs.IsEnabled),
            { OsBuild: < MINIMUM_BUILD } => nameof(HudBackdropInputs.OsBuild),
            { TransparencyEnabled: false } => nameof(HudBackdropInputs.TransparencyEnabled),
            { EnergySaverActive: true } => nameof(HudBackdropInputs.EnergySaverActive),
            { HighContrast: true } => nameof(HudBackdropInputs.HighContrast),
            _ => AVAILABLE
        };
}
