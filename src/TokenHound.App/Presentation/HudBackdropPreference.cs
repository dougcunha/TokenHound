using System;
using System.ComponentModel;
using TokenHound.App.UI.Placement;

namespace TokenHound.App.Presentation;

/// <summary>
/// Holds the live translucent-background preference so the Settings toggle and the HUD share one observable source,
/// together with the Windows condition that currently keeps the HUD on the solid background.
/// </summary>
public sealed class HudBackdropPreference : INotifyPropertyChanged
{
    /// <summary>Notice shown while Windows transparency effects are off.</summary>
    public const string TRANSPARENCY_NOTICE = "Windows transparency effects are off, so the HUD uses the solid background.";

    /// <summary>Notice shown while energy saver is on.</summary>
    public const string ENERGY_SAVER_NOTICE = "Energy saver is on, so the HUD uses the solid background until it turns off.";

    /// <summary>Notice shown while a high-contrast theme is on.</summary>
    public const string HIGH_CONTRAST_NOTICE = "High contrast is on, so the HUD uses the solid background.";

    /// <summary>Notice shown on a Windows build without the translucent material.</summary>
    public const string OS_BUILD_NOTICE = "The translucent background needs Windows 11, so the HUD uses the solid background.";

    private bool _isEnabled = true;
    private string? _unavailability;

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
            Raise(nameof(IsEnabled));
        }
    }

    /// <summary>
    /// Gets or sets the name of the Windows input that blocks the material regardless of the setting,
    /// or <see langword="null"/> when Windows allows it.
    /// </summary>
    public string? Unavailability
    {
        get
            => _unavailability;
        set
        {

            if (string.Equals(_unavailability, value, StringComparison.OrdinalIgnoreCase))
                return;

            _unavailability = value;
            Raise(nameof(Unavailability));
        }
    }

    /// <summary>Gets the notice explaining why the requested material is not shown, or <see langword="null"/>.</summary>
    public string? Notice
        => _isEnabled ? Describe(_unavailability).Notice : null;

    /// <summary>Gets the Windows settings page that can lift the current block, or <see langword="null"/>.</summary>
    public string? SystemSettingsUri
        => _isEnabled ? Describe(_unavailability).Uri : null;

    private static (string? Notice, string? Uri) Describe(string? reason)
        => reason switch
        {
            nameof(HudBackdropInputs.TransparencyEnabled) => (TRANSPARENCY_NOTICE, "ms-settings:colors"),
            nameof(HudBackdropInputs.EnergySaverActive) => (ENERGY_SAVER_NOTICE, "ms-settings:batterysaver"),
            nameof(HudBackdropInputs.HighContrast) => (HIGH_CONTRAST_NOTICE, "ms-settings:easeofaccess-highcontrast"),
            nameof(HudBackdropInputs.OsBuild) => (OS_BUILD_NOTICE, null),
            _ => (null, null)
        };

    private void Raise(string propertyName)
    {

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Notice)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SystemSettingsUri)));
    }
}
