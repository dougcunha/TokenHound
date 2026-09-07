using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TokenHound.App.Interop;
using TokenHound.App.Presentation;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Modeless dialog displaying application information and running build version.
/// </summary>
public sealed partial class AboutWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AboutWindow"/> class using current application metadata.
    /// </summary>
    public AboutWindow()
        : this(ApplicationInfo.Current)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutWindow"/> class with explicit application metadata.
    /// </summary>
    /// <param name="applicationInfo">The application metadata to display.</param>
    public AboutWindow(ApplicationInfo applicationInfo)
    {

        InitializeComponent();

        DataContext = applicationInfo ?? ApplicationInfo.Current;
        SourceInitialized += OnSourceInitialized;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {

        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {

        var hwnd = new WindowInteropHelper(this).Handle;
        WindowPlacement.EnableDarkMode(hwnd);
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {

        Close();
    }
}
