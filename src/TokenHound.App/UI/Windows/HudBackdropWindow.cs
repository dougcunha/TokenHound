using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Owns the raw backdrop companion: its window, host-backdrop sprite, contour region, and place directly below the HUD.
/// </summary>
internal sealed class HudBackdropWindow : IDisposable
{

    private const double FLATTENING_TOLERANCE = 0.25;
    private IntPtr _handle;
    private HudBackdropComposition? _composition;
    private HudShadowInterop.Bounds? _bounds;
    private Geometry? _contour;
    private Matrix _transform;

    /// <summary>Shows the material at the HUD bounds, clipped to the contour in window pixels.</summary>
    /// <param name="hud">The HUD window the companion stays below.</param>
    /// <param name="contour">The painted contour in decorator coordinates.</param>
    /// <param name="pixelTransform">Maps decorator coordinates to HUD window pixels.</param>
    /// <param name="bounds">The HUD window bounds in screen pixels.</param>
    internal void Synchronize(IntPtr hud, Geometry contour, Matrix pixelTransform, HudShadowInterop.Bounds bounds)
    {

        EnsureCreated();

        if (_bounds is not { } previous || previous.Width != bounds.Width || previous.Height != bounds.Height)
            _composition!.Resize(bounds.Width, bounds.Height);

        if (!ReferenceEquals(_contour, contour) || _transform != pixelTransform)
            HudBackdropInterop.SetRegion(_handle, Flatten(contour, pixelTransform));

        HudBackdropInterop.Place(_handle, hud, bounds);
        _bounds = bounds;
        _contour = contour;
        _transform = pixelTransform;
    }

    /// <summary>Hides the companion without releasing it.</summary>
    internal void Hide()
    {

        if (_handle != IntPtr.Zero)
            HudBackdropInterop.Hide(_handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        _composition?.Dispose();
        _composition = null;

        if (_handle != IntPtr.Zero)
            HudBackdropInterop.Destroy(_handle);

        _handle = IntPtr.Zero;
        _bounds = null;
        _contour = null;
    }

    /// <summary>Flattens the contour into one polygon in window pixels.</summary>
    internal static HudBackdropInterop.NativePoint[] Flatten(Geometry contour, Matrix transform)
    {

        var geometry = contour.Clone();
        geometry.Transform = new MatrixTransform(transform);
        var flattened = geometry.GetFlattenedPathGeometry(FLATTENING_TOLERANCE, ToleranceType.Absolute);

        if (flattened.Figures.Count != 1)
            throw new InvalidOperationException("The HUD contour must flatten to one figure.");

        var figure = flattened.Figures[0];
        var points = new List<HudBackdropInterop.NativePoint> { ToNative(figure.StartPoint) };

        foreach (var segment in figure.Segments)
            points.AddRange(Points(segment).Select(ToNative));

        return [.. points];
    }

    private void EnsureCreated()
    {

        if (_handle != IntPtr.Zero)
            return;

        _handle = HudBackdropInterop.Create();
        _composition = HudBackdropComposition.Create(_handle);
    }

    private static IEnumerable<Point> Points(PathSegment segment)
        => segment switch
        {
            PolyLineSegment polyline => polyline.Points,
            LineSegment line => [line.Point],
            _ => throw new InvalidOperationException($"Unexpected flattened segment {segment.GetType().Name}.")
        };

    private static HudBackdropInterop.NativePoint ToNative(Point point)
        => new() { X = (int)Math.Round(point.X), Y = (int)Math.Round(point.Y) };
}
