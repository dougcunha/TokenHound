namespace TokenHound.App.UI.Placement;

/// <summary>A straight or cubic path segment, including its exposed-stroke policy.</summary>
internal sealed record HudContourSegment
{

    /// <summary>Gets the segment endpoint.</summary>
    public required HudContourPoint End { get; init; }

    /// <summary>Gets the first cubic control point, or null for a straight segment.</summary>
    public HudContourPoint? Control1 { get; init; }

    /// <summary>Gets the second cubic control point, or null for a straight segment.</summary>
    public HudContourPoint? Control2 { get; init; }

    /// <summary>Gets whether the segment borders exposed space and receives a stroke.</summary>
    public required bool IsStroked { get; init; }
}
