using System;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Placement;

/// <summary>
/// Maps a placement mode to the screen edge the capsule hugs and whether it stacks providers vertically.
/// </summary>
public static class HudEdgeLayout
{
    /// <summary>
    /// Gets the docked edge and orientation for the specified placement mode.
    /// </summary>
    /// <param name="mode">The placement mode.</param>
    /// <returns>The docked edge, or <see cref="HudEdge.None"/> for Free, and whether the capsule is vertical.</returns>
    public static (HudEdge Edge, bool IsVertical) For(HudDockMode mode)
        => mode switch
        {
            HudDockMode.TopLeft or HudDockMode.TopCenter or HudDockMode.TopRight => (HudEdge.Top, false),
            HudDockMode.LeftEdge => (HudEdge.Left, true),
            HudDockMode.RightEdge => (HudEdge.Right, true),
            HudDockMode.Free => (HudEdge.None, false),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown HUD placement mode.")
        };
}
