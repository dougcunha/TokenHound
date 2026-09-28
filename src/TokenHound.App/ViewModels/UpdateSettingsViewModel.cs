using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using TokenHound.Infrastructure.Configuration;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Presentation model for the "Updates" settings tab: enables or disables automatic update checks and edits the
/// check interval in hours (0 turns periodic checks off), persisted to the <c>Update</c> section.
/// </summary>
public sealed class UpdateSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Validation error shown when the interval is not a whole number from 0 to 720.</summary>
    public const string INTERVAL_ERROR = "Enter a whole number of hours from 0 to 720 (0 turns periodic checks off).";

    private const string PERSIST_ERROR = "Failed to persist update settings.";

    private readonly UpdateSettingsStore _store;
    private readonly RelayCommand _applyCommand;

    private bool _isEnabled, _persistedEnabled, _isApplied;
    private string _intervalHoursText = string.Empty, _persistedIntervalText = string.Empty;
    private int _intervalHours;

    /// <summary>Initializes a new instance of the <see cref="UpdateSettingsViewModel"/> class.</summary>
    /// <param name="store">The store persisting the <c>Update</c> section.</param>
    public UpdateSettingsViewModel(UpdateSettingsStore store)
    {

        ArgumentNullException.ThrowIfNull(store);

        _store = store;
        _applyCommand = new RelayCommand(Apply, () => CanApply);

        var settings = _store.Load();

        _persistedEnabled = settings.IsEnabled;
        _persistedIntervalText = settings.IntervalHours.ToString(CultureInfo.InvariantCulture);
        _isEnabled = _persistedEnabled;
        _intervalHoursText = _persistedIntervalText;
        Validate();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets a value indicating whether automatic update checks are enabled.</summary>
    public bool IsEnabled
    {
        get
            => _isEnabled;
        set
        {

            if (_isEnabled == value)
                return;

            _isEnabled = value;
            Edited();
        }
    }

    /// <summary>Gets or sets the text of the check interval in hours.</summary>
    public string IntervalHoursText
    {
        get
            => _intervalHoursText;
        set
        {

            var text = value ?? string.Empty;

            if (string.Equals(_intervalHoursText, text, StringComparison.Ordinal))
                return;

            _intervalHoursText = text;
            Edited();
        }
    }

    /// <summary>Gets the validation error for the interval, or <see langword="null"/> when valid.</summary>
    public string? IntervalError { get; private set; }

    /// <summary>Gets the error of the most recent failed Apply, or <see langword="null"/>.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Gets a value indicating whether the most recent Apply succeeded and nothing was edited since.</summary>
    public bool IsApplied
        => _isApplied;

    /// <summary>Gets a value indicating whether the inputs differ from the persisted values.</summary>
    public bool IsDirty
        => _isEnabled != _persistedEnabled
            || !string.Equals(_intervalHoursText, _persistedIntervalText, StringComparison.Ordinal);

    /// <summary>Gets a value indicating whether the inputs are valid and changed.</summary>
    public bool CanApply
        => IntervalError is null && IsDirty;

    /// <summary>Gets the command executing <see cref="Apply"/>.</summary>
    public ICommand ApplyCommand
        => _applyCommand;

    /// <summary>Persists the enable flag and interval, preserving the skipped version; the next scheduler tick uses them.</summary>
    public void Apply()
    {

        if (!CanApply)
            return;

        var current = _store.Load();

        if (!_store.Save(current with { Enabled = _isEnabled, CheckIntervalHours = _intervalHours }))
        {
            ApplyError = PERSIST_ERROR;
            Refresh();

            return;
        }

        _persistedEnabled = _isEnabled;
        _persistedIntervalText = _intervalHoursText;
        ApplyError = null;
        _isApplied = true;
        Refresh();
    }

    private void Edited()
    {

        ApplyError = null;
        _isApplied = false;
        Validate();
    }

    private void Validate()
    {

        var valid = int.TryParse(_intervalHoursText, NumberStyles.Integer, CultureInfo.InvariantCulture, out _intervalHours)
            && _intervalHours is >= 0 and <= UpdateSettings.MAXIMUM_INTERVAL_HOURS;

        IntervalError = valid ? null : INTERVAL_ERROR;
        Refresh();
    }

    private void Refresh()
    {

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        _applyCommand.RaiseCanExecuteChanged();
    }

    private sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
            => canExecute?.Invoke() ?? true;

        public void Execute(object? parameter)
            => execute();

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
