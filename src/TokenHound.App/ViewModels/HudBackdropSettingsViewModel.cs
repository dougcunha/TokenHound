using Serilog;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using TokenHound.App.Presentation;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presentation model for the translucent background toggle of the "General" settings tab.
/// Toggling or moving the transparency slider saves the preference and updates the live HUD immediately;
/// a failed save restores the previous value.
/// The card binds <see cref="Preference"/> for the live notice shown while Windows keeps the HUD solid.
/// </summary>
public sealed class HudBackdropSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Error shown when the preference could not be saved.</summary>
    public const string APPLY_ERROR = "Failed to save the translucent background setting.";

    private static readonly ILogger LOGGER = Log.ForContext<HudBackdropSettingsViewModel>();

    private readonly Func<HudBackdropSettings, bool> _save;
    private readonly HudBackdropPreference _preference;
    private readonly Action<string> _openUri;
    private bool _isEnabled;
    private int _transparency;

    /// <summary>Initializes a new instance of the <see cref="HudBackdropSettingsViewModel"/> class.</summary>
    /// <param name="settings">The persisted preference used as the baseline.</param>
    /// <param name="save">Persists the preference and reports whether it was saved.</param>
    /// <param name="preference">The live preference the HUD observes.</param>
    /// <param name="openUri">Opens a Windows settings page; defaults to the shell.</param>
    public HudBackdropSettingsViewModel(
        HudBackdropSettings settings,
        Func<HudBackdropSettings, bool> save,
        HudBackdropPreference preference,
        Action<string>? openUri = null)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(preference);

        _save = save;
        _preference = preference;
        _openUri = openUri ?? OpenWithShell;
        _isEnabled = settings.IsEnabled;
        _transparency = settings.TransparencyPercent;
        OpenSystemSettingsCommand = new RelayCommand(OpenSystemSettings);
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

            if (_isEnabled == value || !TrySave(value, _transparency))
                return;

            _isEnabled = value;
            _preference.IsEnabled = value;
            LOGGER.Debug("Applied HUD backdrop preference {IsEnabled}", value);
            Refresh();
        }
    }

    /// <summary>Gets the lowest transparency the slider offers, in percent.</summary>
    public int Minimum
        => HudBackdropSettings.MINIMUM_TRANSPARENCY;

    /// <summary>Gets the highest transparency the slider offers, in percent.</summary>
    public int Maximum
        => HudBackdropSettings.MAXIMUM_TRANSPARENCY;

    /// <summary>Gets or sets the transparency of the capsule tint in percent; setting it applies immediately.</summary>
    public int Transparency
    {
        get
            => _transparency;
        set
        {

            var transparency = Math.Clamp(value, Minimum, Maximum);

            if (_transparency == transparency || !TrySave(_isEnabled, transparency))
                return;

            _transparency = transparency;
            _preference.Transparency = transparency;
            LOGGER.Debug("Applied HUD backdrop transparency {Transparency}", transparency);
            Refresh();
        }
    }

    /// <summary>Gets the error of the most recent failed save, or <see langword="null"/>.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Gets the live preference whose notice explains why Windows keeps the HUD on the solid background.</summary>
    public HudBackdropPreference Preference
        => _preference;

    /// <summary>Gets the command that opens the Windows settings page able to lift the current block.</summary>
    public ICommand OpenSystemSettingsCommand { get; }

    private void Refresh()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));

    private bool TrySave(bool enabled, int transparency)
    {

        if (_save(new HudBackdropSettings { Enabled = enabled, Transparency = transparency }))
        {

            ApplyError = null;

            return true;
        }

        LOGGER.Warning("Unable to persist HUD backdrop preference {IsEnabled} {Transparency}", enabled, transparency);
        ApplyError = APPLY_ERROR;
        Refresh();

        return false;
    }

    private void OpenSystemSettings()
    {

        if (_preference.SystemSettingsUri is not { } uri)
            return;

        try
        {

            _openUri(uri);
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {

            LOGGER.Warning(exception, "Unable to open Windows settings page {Uri}", uri);
        }
    }

    private static void OpenWithShell(string uri)
        => Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true })?.Dispose();

    private sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
            => true;

        public void Execute(object? parameter)
            => execute();
    }
}
