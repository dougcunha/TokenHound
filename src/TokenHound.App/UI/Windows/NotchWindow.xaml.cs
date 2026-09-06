using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Floating, non-activating screen-edge HUD capsule displaying provider telemetry rings.
/// </summary>
public sealed partial class NotchWindow : Window
{
    private const double TOP_OFFSET = 8.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotchWindow"/> class.
    /// </summary>
    public NotchWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;

        if (hwnd != IntPtr.Zero)
            WindowStyles.EnableNonActivating(hwnd);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {

        RepositionTopCenter();
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {

        RepositionTopCenter();
    }

    private void OnMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {

        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void RepositionTopCenter()
    {

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - ActualWidth) / 2.0);
        Top = workArea.Top + TOP_OFFSET;
    }
}
