using System.Collections.Generic;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.Presentation;

/// <summary>
/// Where the HUD is when the user picks a placement mode: the connected displays, the display hosting the window,
/// and the window position in device-independent pixels.
/// </summary>
public sealed record HudPlacementContext
{
    /// <summary>
    /// Gets the connected displays.
    /// </summary>
    public IReadOnlyList<DisplayInfo> Displays { get; init; } = [];

    /// <summary>
    /// Gets the display currently hosting the HUD window, or <see langword="null"/> when unknown.
    /// </summary>
    public DisplayInfo? Hosting { get; init; }

    /// <summary>
    /// Gets the current horizontal window offset in device-independent pixels.
    /// </summary>
    public double Left { get; init; }

    /// <summary>
    /// Gets the current vertical window offset in device-independent pixels.
    /// </summary>
    public double Top { get; init; }
}
