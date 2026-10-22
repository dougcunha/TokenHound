using System;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Placement;

/// <summary>
/// Computes HUD capsule placement on the screen edge and keeps restored positions visible.
/// </summary>
public static class NotchPlacement
{
    /// <summary>
    /// Computes the placement that centers the capsule on the top edge of the work area.
    /// </summary>
    /// <param name="workArea">The work area hosting the capsule.</param>
    /// <param name="windowWidth">The current capsule window width.</param>
    /// <returns>The centered left and top offsets.</returns>
    public static (double Left, double Top) CenterOnTopEdge(ScreenBounds workArea, double windowWidth)
    {

        ArgumentNullException.ThrowIfNull(workArea);

        return (workArea.Left + ((workArea.Width - windowWidth) / 2.0), workArea.Top);
    }

    /// <summary>
    /// Clamps a placement so the capsule stays fully inside the supplied bounds.
    /// </summary>
    /// <param name="bounds">The region the capsule must remain within.</param>
    /// <param name="left">The candidate horizontal offset.</param>
    /// <param name="top">The candidate vertical offset.</param>
    /// <param name="windowWidth">The current capsule window width.</param>
    /// <param name="windowHeight">The current capsule window height.</param>
    /// <returns>The clamped left and top offsets.</returns>
    public static (double Left, double Top) Clamp(
        ScreenBounds bounds,
        double left,
        double top,
        double windowWidth,
        double windowHeight
    )
    {

        ArgumentNullException.ThrowIfNull(bounds);

        var maxLeft = Math.Max(bounds.Left, bounds.Right - windowWidth);
        var maxTop = Math.Max(bounds.Top, bounds.Bottom - windowHeight);

        return (Math.Clamp(left, bounds.Left, maxLeft), Math.Clamp(top, bounds.Top, maxTop));
    }

    /// <summary>
    /// Computes the window offset that docks the capsule flush with an edge of the work area.
    /// Centering floors the remainder; a window larger than the work area starts at its origin on that axis.
    /// </summary>
    /// <param name="mode">The docked placement mode; <see cref="HudDockMode.Free"/> is not accepted.</param>
    /// <param name="workArea">The work area hosting the capsule, in the same units as the window size.</param>
    /// <param name="windowWidth">The current window width.</param>
    /// <param name="windowHeight">The current window height.</param>
    /// <param name="edgePosition">
    /// The position along the edge from 0 to 1, or <see langword="null"/> to use the default position of the mode.
    /// </param>
    /// <returns>The docked left and top offsets.</returns>
    public static (double Left, double Top) Dock(
        HudDockMode mode,
        ScreenBounds workArea,
        double windowWidth,
        double windowHeight,
        double? edgePosition = null
    )
    {

        ArgumentNullException.ThrowIfNull(workArea);

        var (left, top) = DefaultDock(mode, workArea, windowWidth, windowHeight);

        if (edgePosition is double position)
        {

            var (start, travel, vertical) = Track(mode, workArea, windowWidth, windowHeight);
            var along = start + Math.Round(Math.Clamp(position, 0, 1) * travel);
            (left, top) = vertical ? (left, along) : (along, top);
        }

        return (Math.Max(workArea.Left, left), Math.Max(workArea.Top, top));
    }

    /// <summary>
    /// Constrains a proposed window position to the docked edge: the window keeps its edge and only slides along it,
    /// snapping to an end of the edge when it is within the snap distance.
    /// </summary>
    /// <param name="mode">The docked placement mode; <see cref="HudDockMode.Free"/> is not accepted.</param>
    /// <param name="workArea">The work area hosting the capsule, in the same units as the window size.</param>
    /// <param name="windowWidth">The current window width.</param>
    /// <param name="windowHeight">The current window height.</param>
    /// <param name="left">The proposed horizontal offset.</param>
    /// <param name="top">The proposed vertical offset.</param>
    /// <param name="snapDistance">The distance from an end of the edge that snaps the window to that end.</param>
    /// <returns>The constrained left and top offsets.</returns>
    public static (double Left, double Top) Slide(
        HudDockMode mode,
        ScreenBounds workArea,
        double windowWidth,
        double windowHeight,
        double left,
        double top,
        double snapDistance
    )
    {

        ArgumentNullException.ThrowIfNull(workArea);

        var (start, travel, vertical) = Track(mode, workArea, windowWidth, windowHeight);
        var offset = Math.Clamp((vertical ? top : left) - start, 0, travel);

        if (offset <= snapDistance)
            offset = 0;
        else if (travel - offset <= snapDistance)
            offset = travel;

        var (dockLeft, dockTop) = DefaultDock(mode, workArea, windowWidth, windowHeight);

        return vertical ? (dockLeft, start + offset) : (start + offset, dockTop);
    }

    /// <summary>
    /// Computes the position along the docked edge, from 0 to 1, of a window at the specified offsets.
    /// </summary>
    /// <param name="mode">The docked placement mode; <see cref="HudDockMode.Free"/> is not accepted.</param>
    /// <param name="workArea">The work area hosting the capsule, in the same units as the window size.</param>
    /// <param name="windowWidth">The current window width.</param>
    /// <param name="windowHeight">The current window height.</param>
    /// <param name="left">The window horizontal offset.</param>
    /// <param name="top">The window vertical offset.</param>
    /// <returns>The edge position; 0.5 when the window fills the edge and cannot slide.</returns>
    public static double EdgePosition(
        HudDockMode mode,
        ScreenBounds workArea,
        double windowWidth,
        double windowHeight,
        double left,
        double top
    )
    {

        ArgumentNullException.ThrowIfNull(workArea);

        var (start, travel, vertical) = Track(mode, workArea, windowWidth, windowHeight);

        if (travel <= 0)
            return 0.5;

        return Math.Clamp(((vertical ? top : left) - start) / travel, 0, 1);
    }

    // The edge track: where sliding starts, how far the window can slide, and whether it slides vertically.
    private static (double Start, double Travel, bool Vertical) Track(
        HudDockMode mode,
        ScreenBounds workArea,
        double windowWidth,
        double windowHeight
    )
    {

        var (edge, vertical) = HudEdgeLayout.For(mode);

        if (edge == HudEdge.None)
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Only docked modes have an edge placement.");

        return vertical
            ? (workArea.Top, Math.Max(0, workArea.Height - windowHeight), true)
            : (workArea.Left, Math.Max(0, workArea.Width - windowWidth), false);
    }

    private static (double Left, double Top) DefaultDock(
        HudDockMode mode,
        ScreenBounds workArea,
        double windowWidth,
        double windowHeight
    )
    {

        var left = mode switch
        {
            HudDockMode.TopLeft or HudDockMode.LeftEdge => workArea.Left,
            HudDockMode.TopCenter => workArea.Left + Math.Floor((workArea.Width - windowWidth) / 2.0),
            HudDockMode.TopRight or HudDockMode.RightEdge => workArea.Right - windowWidth,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Only docked modes have an edge placement.")
        };

        var top = mode is HudDockMode.LeftEdge or HudDockMode.RightEdge
            ? workArea.Top + Math.Floor((workArea.Height - windowHeight) / 2.0)
            : workArea.Top;

        return (Math.Max(workArea.Left, left), Math.Max(workArea.Top, top));
    }
}
