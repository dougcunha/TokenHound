using System.ComponentModel;

namespace TokenHound.App.Presentation;

/// <summary>
/// Holds the live translucent-background preference so the Settings toggle and the HUD share one observable source.
/// </summary>
public sealed class HudBackdropPreference : INotifyPropertyChanged
{
    private bool _isEnabled = true;

    /// <summary>
    /// Gets the shared instance used by the application wiring.
    /// </summary>
    public static HudBackdropPreference Current { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets a value indicating whether the translucent background is requested.
    /// </summary>
    public bool IsEnabled
    {
        get
            => _isEnabled;
        set
        {

            if (_isEnabled == value)
                return;

            _isEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }
}
