using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Placement;

/// <summary>The closed contour and its provider-body bounds for one arranged HUD.</summary>
internal sealed record HudContourFrame
{

    /// <summary>Gets the placement used to construct the contour.</summary>
    public required HudDockMode Mode { get; init; }

    /// <summary>Gets the arranged host width.</summary>
    public required double Width { get; init; }

    /// <summary>Gets the arranged host height.</summary>
    public required double Height { get; init; }

    /// <summary>Gets the body origin, excluding decorative continuations.</summary>
    public required HudContourPoint BodyOrigin { get; init; }

    /// <summary>Gets the body width, including the caller's provider padding.</summary>
    public required double BodyWidth { get; init; }

    /// <summary>Gets the body height, including the caller's provider padding.</summary>
    public required double BodyHeight { get; init; }

    /// <summary>Gets the first path point.</summary>
    public required HudContourPoint Start { get; init; }

    /// <summary>Gets the immutable segments, ending explicitly at the first point.</summary>
    public required IReadOnlyList<HudContourSegment> Segments { get; init; }

    /// <summary>Gets the painted stroke thickness.</summary>
    public required double StrokeThickness { get; init; }
}
