namespace TokenHound.Infrastructure.Configuration;

/// <summary>
/// Placement mode of the HUD capsule: docked to a screen edge of the preferred display, or free at stored coordinates.
/// </summary>
public enum HudDockMode
{
    /// <summary>
    /// Horizontal capsule flush with the top-left corner of the work area.
    /// </summary>
    TopLeft,

    /// <summary>
    /// Horizontal capsule centered on the top edge of the work area.
    /// </summary>
    TopCenter,

    /// <summary>
    /// Horizontal capsule flush with the top-right corner of the work area.
    /// </summary>
    TopRight,

    /// <summary>
    /// Vertical capsule centered on the left edge of the work area.
    /// </summary>
    LeftEdge,

    /// <summary>
    /// Vertical capsule centered on the right edge of the work area.
    /// </summary>
    RightEdge,

    /// <summary>
    /// Capsule at the coordinates where the user dropped it.
    /// </summary>
    Free
}
