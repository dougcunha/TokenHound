namespace TokenHound.App.UI.Placement;

/// <summary>Availability inputs that decide whether the HUD may show the Acrylic material.</summary>
internal sealed record HudBackdropInputs
{
    /// <summary>Gets a value indicating whether the user setting requests the translucent background.</summary>
    public required bool IsEnabled { get; init; }

    /// <summary>Gets the Windows build number.</summary>
    public required int OsBuild { get; init; }

    /// <summary>Gets a value indicating whether Windows transparency effects are on.</summary>
    public required bool TransparencyEnabled { get; init; }

    /// <summary>Gets a value indicating whether energy saver (battery saver) is active.</summary>
    public required bool EnergySaverActive { get; init; }

    /// <summary>Gets a value indicating whether a high-contrast theme is active.</summary>
    public required bool HighContrast { get; init; }
}
