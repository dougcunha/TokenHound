using System;
using System.Windows.Interop;
using System.Windows.Media;
using TokenHound.App.Interop;
using TokenHound.App.UI.Placement;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private const int WM_MOVING = 0x0216;
    private const double EDGE_SNAP_DISTANCE = 16;

    private EdgeTrack? _edgeDrag;

    // Keeps a docked HUD on its edge while DragMove runs: the proposed rectangle only slides along the edge.
    private bool ConstrainDockedMove(IntPtr lParam)
    {

        if (_edgeDrag is not { } track)
            return false;

        var proposed = MovingRect.Read(lParam);
        var (left, top) = NotchPlacement.Slide(
            track.Mode,
            track.WorkArea,
            proposed.Width,
            proposed.Height,
            proposed.Left,
            proposed.Top,
            track.SnapDistance
        );

        MovingRect.Write(lParam, proposed, (int)left, (int)top);

        return true;
    }

    private void RecordEdgeDrop(EdgeTrack track)
    {

        var window = WindowPlacement.GetWindowPixelBounds(new WindowInteropHelper(this).Handle);

        if (_placement is null || window is null)
            return;

        var position = NotchPlacement.EdgePosition(
            track.Mode,
            track.WorkArea,
            window.Width,
            window.Height,
            window.Left,
            window.Top
        );

        _placement.RecordEdgeDrag(position);
    }

    // Resolves the edge a docked HUD slides along, in physical pixels; null when the HUD is Free.
    private EdgeTrack? CreateEdgeTrack()
    {

        var displays = Displays?.GetDisplays() ?? [];

        if (_placement is null || _placement.Mode == HudDockMode.Free || displays.Count == 0)
            return null;

        var target = DisplayResolver.Resolve(_placement.Current.Display, displays).Target;
        var snapDistance = EDGE_SNAP_DISTANCE * VisualTreeHelper.GetDpi(this).DpiScaleX;

        return new EdgeTrack(_placement.Mode, target.WorkArea, snapDistance);
    }

    private sealed record EdgeTrack(HudDockMode Mode, ScreenBounds WorkArea, double SnapDistance);
}
