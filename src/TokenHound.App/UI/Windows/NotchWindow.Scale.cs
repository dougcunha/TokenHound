using System.ComponentModel;
using TokenHound.App.Presentation;

namespace TokenHound.App.UI.Windows;

public sealed partial class NotchWindow
{
    private const double BASE_MIN_WIDTH = 180;
    private const double BASE_MIN_HEIGHT = 48;

    private void InitializeScale()
    {

        HudScale.Current.PropertyChanged += OnHudScaleChanged;
        Closed += OnScaleWindowClosed;
        ApplyScaledMinimums();
    }

    private void OnHudScaleChanged(object? sender, PropertyChangedEventArgs e)
    {

        if (e.PropertyName == nameof(HudScale.Factor))
            ApplyScaledMinimums();
    }

    private void OnScaleWindowClosed(object? sender, EventArgs e)
        => HudScale.Current.PropertyChanged -= OnHudScaleChanged;

    private void ApplyScaledMinimums()
    {

        var factor = HudScale.Current.Factor;
        var (minWidth, minHeight) = HudDockLayout.Current.IsVertical
            ? (BASE_MIN_HEIGHT, BASE_MIN_WIDTH)
            : (BASE_MIN_WIDTH, BASE_MIN_HEIGHT);

        MinWidth = minWidth * factor;
        MinHeight = minHeight * factor;
    }
}
