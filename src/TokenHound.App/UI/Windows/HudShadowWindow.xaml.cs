using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>Owns the input-transparent shadow surface outside the painted HUD.</summary>
public sealed partial class HudShadowWindow : Window
{

    private const int WM_MOUSEACTIVATE = 0x0021;
    private const int MA_NOACTIVATE = 3;
    private HwndSource? _source;

    /// <summary>Initializes a passive shadow window.</summary>
    public HudShadowWindow()
    {

        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
    }

    internal void SetContour(Geometry envelope, Matrix transform, Size size)
    {

        var transformed = envelope.Clone();
        transformed.Transform = new MatrixTransform(transform);
        transformed.Freeze();
        var mask = Geometry.Combine(
            new RectangleGeometry(new Rect(size)),
            transformed,
            GeometryCombineMode.Exclude,
            null
        );
        mask.Freeze();
        ShadowSource.Data = envelope;
        ShadowSource.RenderTransform = transformed.Transform;
        ShadowMask.Clip = mask;
    }

    private void OnSourceInitialized(object? sender, EventArgs args)
    {

        var handle = new WindowInteropHelper(this).Handle;
        HudShadowInterop.EnablePassive(handle);
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
    }

    private void OnClosed(object? sender, EventArgs args)
    {

        _source?.RemoveHook(WndProc);
        _source = null;
        SourceInitialized -= OnSourceInitialized;
        Closed -= OnClosed;
    }

    private static IntPtr WndProc(IntPtr handle, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {

        if (message != WM_MOUSEACTIVATE)
            return IntPtr.Zero;

        handled = true;

        return new IntPtr(MA_NOACTIVATE);
    }
}
