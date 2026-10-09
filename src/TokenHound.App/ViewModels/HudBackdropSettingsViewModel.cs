using Serilog;
using System;
using System.ComponentModel;
using TokenHound.App.Presentation;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presentation model for the translucent background toggle of the "General" settings tab.
/// Toggling saves the preference and updates the live HUD immediately; a failed save restores the previous value.
/// </summary>
public sealed class HudBackdropSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Error shown when the preference could not be saved.</summary>
    public const string APPLY_ERROR = "Failed to save the translucent background setting.";

    private static readonly ILogger LOGGER = Log.ForContext<HudBackdropSettingsViewModel>();

    private readonly Func<HudBackdropSettings, bool> _save;
    private readonly HudBackdropPreference _preference;
    private bool _isEnabled;

    /// <summary>Initializes a new instance of the <see cref="HudBackdropSettingsViewModel"/> class.</summary>
    /// <param name="settings">The persisted preference used as the baseline.</param>
    /// <param name="save">Persists the preference and reports whether it was saved.</param>
    /// <param name="preference">The live preference the HUD observes.</param>
    public HudBackdropSettingsViewModel(HudBackdropSettings settings, Func<HudBackdropSettings, bool> save, HudBackdropPreference preference)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(preference);

        _save = save;
        _preference = preference;
        _isEnabled = settings.IsEnabled;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Creates the model over the persisted preference and the live HUD preference.</summary>
    /// <param name="store">The store holding the persisted preference.</param>
    /// <param name="preference">The live preference the HUD observes.</param>
    /// <returns>The model whose baseline is the preference currently stored.</returns>
    public static HudBackdropSettingsViewModel Create(HudBackdropStore store, HudBackdropPreference preference)
    {

        ArgumentNullException.ThrowIfNull(store);

        return new HudBackdropSettingsViewModel(store.Load(), store.Save, preference);
    }

    /// <summary>Gets or sets a value indicating whether the translucent background is on; setting it applies immediately.</summary>
    public bool IsEnabled
    {
        get
            => _isEnabled;
        set
        {

            if (_isEnabled == value)
                return;

            if (!_save(new HudBackdropSettings { Enabled = value }))
            {

                LOGGER.Warning("Unable to persist HUD backdrop preference {IsEnabled}", value);
                ApplyError = APPLY_ERROR;
                Refresh();

                return;
            }

            _isEnabled = value;
            _preference.IsEnabled = value;
            ApplyError = null;
            LOGGER.Debug("Applied HUD backdrop preference {IsEnabled}", value);
            Refresh();
        }
    }

    /// <summary>Gets the error of the most recent failed save, or <see langword="null"/>.</summary>
    public string? ApplyError { get; private set; }

    private void Refresh()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
}
