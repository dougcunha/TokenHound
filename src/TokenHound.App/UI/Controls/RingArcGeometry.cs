using System;
using System.Windows;
using System.Windows.Media;

namespace TokenHound.App.UI.Controls;

/// <summary>Builds the frozen arc geometry that renders quota consumption on a provider ring.</summary>
internal static class RingArcGeometry
{
    private const double CENTER_X = 22.0;
    private const double CENTER_Y = 22.0;
    private const double RADIUS = 18.0;

    /// <summary>Creates the arc sweeping clockwise from twelve o'clock by the consumed fraction.</summary>
    /// <param name="fraction">The quota utilization fraction (0.0 to 1.0).</param>
    /// <returns>The frozen arc geometry, or null when there is nothing to draw.</returns>
    public static Geometry? Create(double fraction)
    {

        if (fraction <= 0.0001)
            return null;

        var clamped = Math.Min(fraction, 1.0);
        var angle = clamped >= 0.9999 ? 359.99 : clamped * 360.0;
        var radians = angle * Math.PI / 180.0;
        var endX = CENTER_X + RADIUS * Math.Sin(radians);
        var endY = CENTER_Y - RADIUS * Math.Cos(radians);

        var segment = new ArcSegment
        {
            Point = new Point(endX, endY),
            Size = new Size(RADIUS, RADIUS),
            RotationAngle = 0.0,
            IsLargeArc = angle > 180.0,
            SweepDirection = SweepDirection.Clockwise,
            IsStroked = true
        };

        var figure = new PathFigure
        {
            StartPoint = new Point(CENTER_X, CENTER_Y - RADIUS),
            Segments = [segment],
            IsClosed = false
        };

        var geometry = new PathGeometry([figure]);
        geometry.Freeze();

        return geometry;
    }
}
