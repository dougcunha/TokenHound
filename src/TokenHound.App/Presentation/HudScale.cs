using System.ComponentModel;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.Presentation;

/// <summary>
/// Holds the live HUD scale so the capsule, status popup, and tooltip card, which live in separate visual trees,
/// can bind to a single observable source.
/// </summary>
public sealed class HudScale : INotifyPropertyChanged
{
    private int _percent = HudSizeSettings.DEFAULT_PERCENT;

    /// <summary>
    /// Gets the shared instance used by XAML bindings and the application wiring.
    /// </summary>
    public static HudScale Current { get; } = new();

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the HUD size in percent; values are clamped and rounded to the accepted step.
    /// </summary>
    public int Percent
    {
        get
            => _percent;
        set
        {

            var normalized = HudSizeSettings.Normalize(value);

            if (normalized == _percent)
                return;

            _percent = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Percent)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Factor)));
        }
    }

    /// <summary>
    /// Gets the scale factor, where 1.0 is the default size.
    /// </summary>
    public double Factor
        => _percent / 100.0;
}
