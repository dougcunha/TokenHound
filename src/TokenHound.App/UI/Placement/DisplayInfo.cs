namespace TokenHound.App.UI.Placement;

/// <summary>
/// Connected display as seen by the HUD placement rules: identity, label data, and work area in physical pixels.
/// </summary>
public sealed record DisplayInfo
{
    /// <summary>
    /// Gets the monitor device path, the primary identity key; the GDI device name when the path is unavailable.
    /// </summary>
    public required string DevicePath { get; init; }

    /// <summary>
    /// Gets the EDID manufacturer and product code as <c>MMMM:PPPP</c> hex, or <see langword="null"/> when not reported.
    /// </summary>
    public string? EdidKey { get; init; }

    /// <summary>
    /// Gets the friendly display name, or <c>Display N</c> when Windows reports none.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the display number Windows shows in display settings.
    /// </summary>
    public required int Number { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is the Windows primary display.
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Gets the work area (display bounds minus taskbar and app bars) in physical pixels.
    /// </summary>
    public required ScreenBounds WorkArea { get; init; }

    /// <summary>
    /// Gets the horizontal resolution in physical pixels.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Gets the vertical resolution in physical pixels.
    /// </summary>
    public required int Height { get; init; }
}
