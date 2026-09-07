namespace TokenHound.App.UI.Placement;

/// <summary>
/// Rectangular screen region expressed in device-independent pixels.
/// </summary>
public sealed record ScreenBounds
{
    /// <summary>
    /// Gets the left edge of the region.
    /// </summary>
    public required double Left { get; init; }

    /// <summary>
    /// Gets the top edge of the region.
    /// </summary>
    public required double Top { get; init; }

    /// <summary>
    /// Gets the horizontal extent of the region.
    /// </summary>
    public required double Width { get; init; }

    /// <summary>
    /// Gets the vertical extent of the region.
    /// </summary>
    public required double Height { get; init; }

    /// <summary>
    /// Gets the right edge of the region.
    /// </summary>
    public double Right
        => Left + Width;

    /// <summary>
    /// Gets the bottom edge of the region.
    /// </summary>
    public double Bottom
        => Top + Height;
}
