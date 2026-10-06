namespace TokenHound.App.UI.Placement;

/// <summary>
/// Display chosen to host the docked HUD, and whether it replaces an unavailable preferred display.
/// </summary>
public sealed record DisplayResolution
{
    /// <summary>
    /// Gets the display that hosts the HUD.
    /// </summary>
    public required DisplayInfo Target { get; init; }

    /// <summary>
    /// Gets a value indicating whether the preferred display was not found and the primary display is used instead.
    /// </summary>
    public bool IsFallback { get; init; }
}
