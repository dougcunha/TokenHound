using System;

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
}
