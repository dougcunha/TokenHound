namespace TokenHound.App.UI.Placement;

/// <summary>A point in the contour's unscaled device-independent coordinate space.</summary>
internal sealed record HudContourPoint
{

    /// <summary>Gets the horizontal coordinate.</summary>
    public required double X { get; init; }

    /// <summary>Gets the vertical coordinate.</summary>
    public required double Y { get; init; }
}
