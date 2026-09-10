using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TokenHound.Core.Policies;
using TokenHound.Infrastructure.Configuration;
using TokenHound.Infrastructure.Engine;

namespace TokenHound.App.ViewModels;

/// <summary>Presentation model coordinating user configuration of polling cadence and HTTP 429 rate-limit retry floors.</summary>
public sealed class CadenceSettingsViewModel : INotifyPropertyChanged
{
    /// <summary>Validation error displayed when active interval is below safety floor or invalid.</summary>
    public const string ACTIVE_INTERVAL_ERROR = "Active interval must be at least 30 seconds.";

    /// <summary>Validation error displayed when idle interval is below active interval or safety floor.</summary>
    public const string IDLE_INTERVAL_ERROR = "Idle interval cannot be shorter than active interval.";

    /// <summary>Validation error displayed when retry floor is below safety floor or invalid.</summary>
    public const string RETRY_FLOOR_ERROR = "Minimum retry floor must be at least 60 seconds to prevent API abuse.";

    private const string REFRESH_PERSIST_ERROR = "Failed to persist refresh cadence settings.";
    private const string RATE_LIMIT_PERSIST_ERROR = "Failed to persist rate limit settings.";

    private readonly UsageStore _usageStore;
    private readonly RefreshSettingsStore _refreshStore;
    private readonly RateLimitSettingsStore _rateLimitStore;
    private readonly RateLimitPolicy _rateLimitPolicy;
    private readonly RelayCommand _applyCommand;

    private string _activeIntervalText = string.Empty, _idleIntervalText = string.Empty, _minimumRetryFloorText = string.Empty;
    private string _persistedActive = string.Empty, _persistedIdle = string.Empty, _persistedRetry = string.Empty;
    private int _activeIntervalSeconds, _idleIntervalSeconds, _minimumRetryFloorSeconds;

    /// <summary>Initializes a new instance of the <see cref="CadenceSettingsViewModel"/> class.</summary>
    /// <param name="usageStore">The central usage store receiving runtime cadence adjustments.</param>
    /// <param name="refreshStore">Optional store for persisting polling cadence settings.</param>
    /// <param name="rateLimitStore">Optional store for persisting rate-limit retry floor settings.</param>
    /// <param name="rateLimitPolicy">Optional isolated runtime rate-limit policy.</param>
    public CadenceSettingsViewModel(
        UsageStore usageStore,
        RefreshSettingsStore? refreshStore = null,
        RateLimitSettingsStore? rateLimitStore = null,
        RateLimitPolicy? rateLimitPolicy = null)
    {

        ArgumentNullException.ThrowIfNull(usageStore);

        _usageStore = usageStore;
        _refreshStore = refreshStore ?? new RefreshSettingsStore();
        _rateLimitStore = rateLimitStore ?? new RateLimitSettingsStore();
        _rateLimitPolicy = rateLimitPolicy ?? new RateLimitPolicy();
        _applyCommand = new RelayCommand(Apply, () => CanApply);

        LoadInitialValues();
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the text representation of the active polling interval in seconds.</summary>
    public string ActiveIntervalText
    {
        get
            => _activeIntervalText;
        set
            => SetText(ref _activeIntervalText, value);
    }

    /// <summary>Gets or sets the active polling interval in seconds.</summary>
    public int ActiveIntervalSeconds
    {
        get
            => _activeIntervalSeconds;
        set
            => ActiveIntervalText = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Gets or sets the text representation of the idle polling interval in seconds.</summary>
    public string IdleIntervalText
    {
        get
            => _idleIntervalText;
        set
            => SetText(ref _idleIntervalText, value);
    }

    /// <summary>Gets or sets the idle polling interval in seconds.</summary>
    public int IdleIntervalSeconds
    {
        get
            => _idleIntervalSeconds;
        set
            => IdleIntervalText = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Gets or sets the text representation of the minimum retry floor in seconds.</summary>
    public string MinimumRetryFloorText
    {
        get
            => _minimumRetryFloorText;
        set
            => SetText(ref _minimumRetryFloorText, value);
    }

    /// <summary>Gets or sets the minimum retry floor in seconds.</summary>
    public int MinimumRetryFloorSeconds
    {
        get
            => _minimumRetryFloorSeconds;
        set
            => MinimumRetryFloorText = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Gets the validation error for the active interval input, or null if valid.</summary>
    public string? ActiveIntervalError { get; private set; }

    /// <summary>Gets the validation error for the idle interval input, or null if valid.</summary>
    public string? IdleIntervalError { get; private set; }

    /// <summary>Gets the validation error for the retry floor input, or null if valid.</summary>
    public string? RetryFloorError { get; private set; }

    /// <summary>Gets a value indicating whether any validation errors currently exist.</summary>
    public bool HasErrors
        => ActiveIntervalError is not null || IdleIntervalError is not null || RetryFloorError is not null;

    /// <summary>Gets a value indicating whether current inputs differ from the last persisted values.</summary>
    public bool IsDirty
        => !string.Equals(_activeIntervalText, _persistedActive, StringComparison.Ordinal)
            || !string.Equals(_idleIntervalText, _persistedIdle, StringComparison.Ordinal)
            || !string.Equals(_minimumRetryFloorText, _persistedRetry, StringComparison.Ordinal);

    /// <summary>Gets a value indicating whether changes are eligible to be applied.</summary>
    public bool CanApply
        => !HasErrors && IsDirty;

    /// <summary>Gets the error description from the most recent failed Apply attempt, or null.</summary>
    public string? ApplyError { get; private set; }

    /// <summary>Gets a value indicating whether the most recent Apply attempt encountered an error.</summary>
    public bool HasApplyError
        => !string.IsNullOrEmpty(ApplyError);

    /// <summary>Gets the command executing <see cref="Apply"/>.</summary>
    public ICommand ApplyCommand
        => _applyCommand;

    /// <summary>Resets all interval and retry inputs to recommended factory default values.</summary>
    public void ResetToDefaults()
    {

        RevertInputs(
            ((int)RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL.TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ((int)RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL.TotalSeconds).ToString(CultureInfo.InvariantCulture),
            ((int)RateLimitPolicy.MINIMUM_RETRY_FLOOR.TotalSeconds).ToString(CultureInfo.InvariantCulture)
        );
    }

    /// <summary>Discards unapplied edits and reverts inputs to the most recently active persisted values.</summary>
    public void Discard()
    {

        RevertInputs(_persistedActive, _persistedIdle, _persistedRetry);
    }

    /// <summary>Persists the cadence and rate-limit settings to configuration stores, then updates runtime policies.</summary>
    public void Apply()
    {

        if (HasErrors || !Persist())
            return;

        _usageStore.UpdateCadence(TimeSpan.FromSeconds(_activeIntervalSeconds), TimeSpan.FromSeconds(_idleIntervalSeconds));
        _rateLimitPolicy.SetEffectiveFloor(TimeSpan.FromSeconds(_minimumRetryFloorSeconds));

        _persistedActive = _activeIntervalText;
        _persistedIdle = _idleIntervalText;
        _persistedRetry = _minimumRetryFloorText;
        Validate();
    }

    private void RevertInputs(string active, string idle, string retry)
    {

        _activeIntervalText = active;
        _idleIntervalText = idle;
        _minimumRetryFloorText = retry;
        SetApplyError(null);
        Validate();
    }

    private void SetText(ref string field, string? value, [CallerMemberName] string? propertyName = null)
    {

        var text = value ?? string.Empty;

        if (string.Equals(field, text, StringComparison.Ordinal))
            return;

        field = text;
        OnPropertyChanged(propertyName);
        SetApplyError(null);
        Validate();
    }

    private bool Persist()
    {

        if (!_refreshStore.Save(new RefreshSettings { ActiveIntervalSeconds = _activeIntervalSeconds, IdleIntervalSeconds = _idleIntervalSeconds }))
        {

            SetApplyError(REFRESH_PERSIST_ERROR);

            return false;
        }

        if (!_rateLimitStore.Save(new RateLimitSettings { MinimumRetryFloorSeconds = _minimumRetryFloorSeconds }))
        {

            SetApplyError(RATE_LIMIT_PERSIST_ERROR);

            return false;
        }

        SetApplyError(null);

        return true;
    }

    private void Validate()
    {

        var activeOk = TryParseFloor(_activeIntervalText, RefreshSettings.MINIMUM_INTERVAL_SECONDS, out _activeIntervalSeconds);
        var idleOk = TryParseFloor(_idleIntervalText, RefreshSettings.MINIMUM_INTERVAL_SECONDS, out _idleIntervalSeconds);
        var retryOk = TryParseFloor(_minimumRetryFloorText, RateLimitSettings.MINIMUM_FLOOR_SECONDS, out _minimumRetryFloorSeconds);

        ActiveIntervalError = activeOk ? null : ACTIVE_INTERVAL_ERROR;
        IdleIntervalError = idleOk && (ActiveIntervalError is not null || _idleIntervalSeconds >= _activeIntervalSeconds) ? null : IDLE_INTERVAL_ERROR;
        RetryFloorError = retryOk ? null : RETRY_FLOOR_ERROR;

        OnPropertyChanged(null);
        _applyCommand.RaiseCanExecuteChanged();
    }

    private static bool TryParseFloor(string text, int floor, out int value)
    {

        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= floor)
            return true;

        value = 0;

        return false;
    }

    private void SetApplyError(string? error)
    {

        if (string.Equals(ApplyError, error, StringComparison.Ordinal))
            return;

        ApplyError = error;
        OnPropertyChanged(nameof(ApplyError));
        OnPropertyChanged(nameof(HasApplyError));
    }

    private void LoadInitialValues()
    {

        var refresh = _refreshStore.Load();
        var rateLimit = _rateLimitStore.Load();

        _persistedActive = (refresh.ActiveIntervalSeconds ?? (int)RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        _persistedIdle = (refresh.IdleIntervalSeconds ?? (int)RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        _persistedRetry = (rateLimit.MinimumRetryFloorSeconds ?? RateLimitSettings.DEFAULT_FLOOR_SECONDS).ToString(CultureInfo.InvariantCulture);

        RevertInputs(_persistedActive, _persistedIdle, _persistedRetry);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
