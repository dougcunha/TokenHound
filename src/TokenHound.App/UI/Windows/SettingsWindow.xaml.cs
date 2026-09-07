using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TokenHound.App.Interop;

namespace TokenHound.App.UI.Windows;

/// <summary>
/// Modeless dialog displaying the application settings surface.
/// </summary>
public sealed partial class SettingsWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsWindow"/> class.
    /// </summary>
    public SettingsWindow()
    {

        InitializeComponent();

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
