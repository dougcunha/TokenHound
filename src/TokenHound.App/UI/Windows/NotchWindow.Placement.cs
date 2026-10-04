using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TokenHound.App.Interop;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private bool _applyingPlacement;

    private void ApplyPlacement()
    {

        if (_applyingPlacement)
            return;

        _applyingPlacement = true;

        try
        {

            var hasPosition = _position.TryGetPosition(out var left, out var top);

            if (!hasPosition)
                (left, top) = NotchPlacement.CenterOnTopEdge(ToBounds(SystemParameters.WorkArea), ActualWidth);

            ApplyMonitorPlacement(left, top);

            if (hasPosition)
                PersistPosition();
        }
        finally
        {

            _applyingPlacement = false;
        }
    }

    private void ApplyMonitorPlacement(double left, double top)
    {

        // Resolve the nearest real monitor after WPF applies the position and any DPI transition.
        (Left, Top) = (left, top);
        var hwnd = new WindowInteropHelper(this).Handle;
        var workArea = WindowPlacement.GetWorkArea(hwnd, VisualTreeHelper.GetDpi(this));

        (Left, Top) = NotchPlacement.Clamp(
            ToBounds(workArea),
            Left,
            Top,
            ActualWidth,
            ActualHeight
        );
    }

    private static ScreenBounds ToBounds(Rect bounds)
        => new()
        {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height
        };
}
