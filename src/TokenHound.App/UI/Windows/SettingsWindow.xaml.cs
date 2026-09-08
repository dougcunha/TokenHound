using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
        Loaded += OnLoaded;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {

        var firstToggle = FindFirstToggle(ProviderItems);

        if (firstToggle is not null)
        {
            firstToggle.Focus();

            return;
        }

        CloseButton.Focus();
    }

    private static CheckBox? FindFirstToggle(DependencyObject root)
    {

        var childCount = VisualTreeHelper.GetChildrenCount(root);

        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);

            if (child is CheckBox toggle)
                return toggle;

            var descendant = FindFirstToggle(child);

            if (descendant is not null)
                return descendant;
        }

        return null;
    }
}
