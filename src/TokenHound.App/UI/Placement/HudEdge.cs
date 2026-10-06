namespace TokenHound.App.UI.Placement;

/// <summary>
/// Screen edge the HUD capsule is docked to.
/// </summary>
public enum HudEdge
{
    /// <summary>
    /// Not docked: the capsule floats at stored coordinates.
    /// </summary>
    None,

    /// <summary>
    /// Docked to the top edge.
    /// </summary>
    Top,

    /// <summary>
    /// Docked to the left edge.
    /// </summary>
    Left,

    /// <summary>
    /// Docked to the right edge.
    /// </summary>
    Right
}
