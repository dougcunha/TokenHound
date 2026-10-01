using Serilog;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using TokenHound.App.Presentation;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presentation model for the HUD size section of the "General" settings tab: a percentage slider with quick-pick presets.
/// The baseline is the persisted size, and Apply saves it and resizes the live HUD through <see cref="HudScale"/>.
/// </summary>
public sealed class HudSizeSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Error shown when the HUD size could not be saved.</summary>
    public const string APPLY_ERROR = "Failed to save the HUD size.";

    /// <summary>The "Small" preset, in percent.</summary>
    public const int SMALL_PERCENT = 75;

    /// <summary>The "Large" preset, in percent.</summary>
    public const int LARGE_PERCENT = 125;

    private static readonly ILogger LOGGER = Log.ForContext<HudSizeSettingsViewModel>();

    private readonly Func<HudSizeSettings, bool> _save;
    private readonly HudScale _scale;
    private readonly RelayCommand _applyCommand;
    private readonly RelayCommand _presetCommand;

    private int _percent, _persistedPercent;
    private bool _isApplied;

    /// <summary>Initializes a new instance of the <see cref="HudSizeSettingsViewModel"/> class.</summary>
    /// <param name="settings">The persisted HUD size used as the baseline.</param>
    /// <param name="save">Persists the HUD size and reports whether it was saved.</param>
    /// <param name="scale">The live HUD scale that Apply updates.</param>
    public HudSizeSettingsViewModel(HudSizeSettings settings, Func<HudSizeSettings, bool> save, HudScale scale)
    {

        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(scale);

        _save = save;
        _scale = scale;
        _applyCommand = new RelayCommand(_ => Apply(), _ => CanApply);
        _presetCommand = new RelayCommand(SetPresetFromParameter);
        _persistedPercent = settings.ResolvedPercent;
        _percent = _persistedPercent;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Creates the model over the persisted HUD size and the live HUD scale.</summary>
    /// <param name="store">The store holding the persisted HUD size.</param>
    /// <param name="scale">The live HUD scale that Apply updates.</param>
    /// <returns>The model whose baseline is the size currently stored.</returns>
    public static HudSizeSettingsViewModel Create(HudSizeStore store, HudScale scale)
    {

        ArgumentNullException.ThrowIfNull(store);

        return new HudSizeSettingsViewModel(store.Load(), store.Save, scale);
    }

    /// <summary>Gets the smallest accepted size, in percent.</summary>
    public int Minimum
        => HudSizeSettings.MINIMUM_PERCENT;

    /// <summary>Gets the largest accepted size, in percent.</summary>
    public int Maximum
        => HudSizeSettings.MAXIMUM_PERCENT;

    /// <summary>Gets the slider tick spacing, in percent.</summary>
    public int Step
        => HudSizeSettings.STEP_PERCENT;

    /// <summary>Gets or sets the chosen HUD size in percent; values are clamped and rounded to the accepted step.</summary>
    public int Percent
    {
        get
            => _percent;
        set
        {

            var normalized = HudSizeSettings.Normalize(value);

            if (_percent == normalized)
                return;

            _percent = normalized;
            ApplyError = null;
            _isApplied = false;
            Refresh();
        }
    }

    /// <summary>Gets the chosen size as text, for example <c>75%</c>.</summary>
    public string Label
        => string.Create(CultureInfo.InvariantCulture, $"{_percent}%");

    /// <summary>Gets the error of the most recent failed Apply, or <see langword="null"/>.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Gets a value indicating whether the most recent Apply succeeded and nothing was edited since.</summary>
    public bool IsApplied
        => _isApplied;

    /// <summary>Gets a value indicating whether the chosen size differs from the persisted one.</summary>
    public bool IsDirty
        => _percent != _persistedPercent;

    /// <summary>Gets a value indicating whether there is a change to apply.</summary>
    public bool CanApply
        => IsDirty;

    /// <summary>Gets the command executing <see cref="Apply"/>.</summary>
    public ICommand ApplyCommand
        => _applyCommand;

    /// <summary>Gets the command choosing a preset size; the parameter is the size in percent.</summary>
    public ICommand PresetCommand
        => _presetCommand;

    /// <summary>Chooses a preset size without applying it.</summary>
    /// <param name="percent">The preset size in percent.</param>
    public void SetPreset(int percent)
        => Percent = percent;

    /// <summary>Persists the chosen size and resizes the live HUD; on failure the persisted baseline is kept.</summary>
    public void Apply()
    {

        if (!CanApply)
            return;

        if (!_save(new HudSizeSettings { Percent = _percent }))
        {

            LOGGER.Warning("Unable to persist HUD size {Percent}", _percent);
            ApplyError = APPLY_ERROR;
            Refresh();

            return;
        }

        _persistedPercent = _percent;
        _scale.Percent = _percent;
        LOGGER.Debug("Applied HUD size {Percent}", _percent);
        _isApplied = true;
        Refresh();
    }

    private void SetPresetFromParameter(object? parameter)
    {

        if (parameter is not null && int.TryParse(Convert.ToString(parameter, CultureInfo.InvariantCulture), out var percent))
            SetPreset(percent);
    }

    private void Refresh()
    {

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        _applyCommand.RaiseCanExecuteChanged();
    }

    private sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter)
            => execute(parameter);

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
