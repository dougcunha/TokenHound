using System;
using System.Windows;
using System.Windows.Interop;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Floating, non-activating screen-edge HUD capsule displaying provider telemetry rings.
/// </summary>
public sealed partial class NotchWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotchWindow"/> class.
    /// </summary>
    public NotchWindow()
    {
        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
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

    private void RepositionTopCenter()
    {

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - ActualWidth) / 2.0);
        Top = workArea.Top;
    }
}
