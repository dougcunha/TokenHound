using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Modeless window listing the status of every enabled provider, grouped by provider family.
/// </summary>
public sealed partial class ProviderStatusWindow : Window
{
    private const string CAPTION_BACKGROUND_KEY = "SurfaceBackgroundColor";
    private const string CAPTION_FOREGROUND_KEY = "TextPrimaryColor";

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderStatusWindow"/> class.
    /// </summary>
    public ProviderStatusWindow()
    {

        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        var background = (Color)FindResource(CAPTION_BACKGROUND_KEY);
        var foreground = (Color)FindResource(CAPTION_FOREGROUND_KEY);

        WindowPlacement.EnableDarkMode(hwnd);
        WindowPlacement.SetCaptionColors(hwnd, background, foreground);
    }
}
