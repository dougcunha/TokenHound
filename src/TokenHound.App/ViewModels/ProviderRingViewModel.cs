using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>Represents the observable view model for an individual provider indicator ring and hover card.</summary>
public sealed class ProviderRingViewModel : INotifyPropertyChanged
{
    private const string DEFAULT_CLAUDE_NAME = "Claude Code";
    private const string DEFAULT_CLAUDE_BADGE = "C";
    private const string DEFAULT_MOCK_NAME = "Mock Provider";
    private const string DEFAULT_MOCK_BADGE = "M";

    private readonly string _providerId;
    private string _providerName;
    private string _providerBadge;
    private double? _usedFraction;
    private ProviderStatus _status;
    private bool _isBusy;
    private double? _sessionUsedFraction;
    private string? _sessionResetText;
    private double? _weeklyUsedFraction;
    private string? _weeklyResetText;
    private string? _activeSessionText;
    private string? _statusMessage;

    /// <summary>Initializes a new instance of the <see cref="ProviderRingViewModel"/> class.</summary>
    /// <param name="providerId">The unique identifier of the provider.</param>
    /// <param name="providerName">Optional display name, or null for default.</param>
    /// <param name="providerBadge">Optional badge glyph, or null for default.</param>
    public ProviderRingViewModel(
        string providerId,
        string? providerName = null,
        string? providerBadge = null)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        _providerId = providerId;
        _providerName = providerName ?? ResolveDefaultName(providerId);
        _providerBadge = providerBadge ?? ResolveDefaultBadge(providerId);
        _status = ProviderStatus.Ok;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the unique identifier of the provider.</summary>
    public string ProviderId
        => _providerId;

    /// <summary>Gets or sets the human-readable display name of the provider.</summary>
    public string ProviderName
    {
        get
            => _providerName;
        set
            => SetProperty(ref _providerName, value);
    }

    /// <summary>Gets or sets the short badge glyph displayed in the center of the ring.</summary>
    public string ProviderBadge
    {
        get
            => _providerBadge;
        set
            => SetProperty(ref _providerBadge, value);
    }

    /// <summary>Gets or sets the primary quota utilization fraction (0.0 to 1.0), or null if unmeasured.</summary>
    public double? UsedFraction
    {
        get
            => _usedFraction;
        set
            => SetProperty(ref _usedFraction, value);
    }

    /// <summary>Gets or sets the operational and health status of the provider.</summary>
    public ProviderStatus Status
    {
        get
            => _status;
        set
            => SetProperty(ref _status, value);
    }

    /// <summary>Gets or sets a value indicating whether a session process is actively executing tasks.</summary>
    public bool IsBusy
    {
        get
            => _isBusy;
        set
            => SetProperty(ref _isBusy, value);
    }

    /// <summary>Gets or sets the rolling session quota fraction, or null if unmeasured.</summary>
    public double? SessionUsedFraction
    {
        get
            => _sessionUsedFraction;
        set
            => SetProperty(ref _sessionUsedFraction, value);
    }

    /// <summary>Gets or sets the formatted reset countdown for the rolling session window.</summary>
    public string? SessionResetText
    {
        get
            => _sessionResetText;
        set
            => SetProperty(ref _sessionResetText, value);
    }

    /// <summary>Gets or sets the weekly quota fraction, or null if unmeasured.</summary>
    public double? WeeklyUsedFraction
    {
        get
            => _weeklyUsedFraction;
        set
            => SetProperty(ref _weeklyUsedFraction, value);
    }

    /// <summary>Gets or sets the formatted reset countdown for the weekly quota window.</summary>
    public string? WeeklyResetText
    {
        get
            => _weeklyResetText;
        set
            => SetProperty(ref _weeklyResetText, value);
    }

    /// <summary>Gets or sets the active session process description text, or null if idle.</summary>
    public string? ActiveSessionText
    {
        get
            => _activeSessionText;
        set
            => SetProperty(ref _activeSessionText, value);
    }

    /// <summary>Gets or sets the actionable status guidance message, or null if normal.</summary>
    public string? StatusMessage
    {
        get
            => _statusMessage;
        set
            => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Updates all observable properties from a domain telemetry snapshot.</summary>
    /// <param name="snapshot">The snapshot containing updated metrics and health status.</param>
    public void UpdateFromSnapshot(Snapshot snapshot)
    {

        ArgumentNullException.ThrowIfNull(snapshot);

        Status = snapshot.Status;
        StatusMessage = ResolveStatusMessage(snapshot);

        var nowUtc = DateTimeOffset.UtcNow;
        var sessionWindow = FindLimitWindow(snapshot.LimitWindows, isSession: true);
        var weeklyWindow = FindLimitWindow(snapshot.LimitWindows, isSession: false);

        UsedFraction = sessionWindow?.UsedFraction;
        SessionUsedFraction = sessionWindow?.UsedFraction;
        WeeklyUsedFraction = weeklyWindow?.UsedFraction;

        var sessionReset = sessionWindow?.ResetTimeUtc ?? snapshot.ActiveBlock?.ResetTimeUtc;
        SessionResetText = FormatResetCountdown(sessionReset, nowUtc);
        WeeklyResetText = FormatResetCountdown(weeklyWindow?.ResetTimeUtc, nowUtc);
    }

    /// <summary>Updates the session activity state and active session description text.</summary>
    /// <param name="isBusy">Whether a session is actively running tasks.</param>
    /// <param name="sessionText">The descriptive session text, or null if idle.</param>
    public void UpdateActivity(bool isBusy, string? sessionText = null)
    {

        IsBusy = isBusy;
        ActiveSessionText = sessionText;
    }

    /// <summary>Updates the session activity state from an agent session model.</summary>
    /// <param name="session">The agent session model, or null if no active session exists.</param>
    public void UpdateActivity(AgentSession? session)
    {

        if (session is null)
        {

            UpdateActivity(false, null);

            return;
        }

        UpdateActivity(session.State == AgentSessionState.Busy, $"PID {session.Pid} • {(session.State == AgentSessionState.Busy ? "Active" : "Idle")}");
    }

    private static LimitWindow? FindLimitWindow(IReadOnlyList<LimitWindow> windows, bool isSession)
    {

        if (windows.Count == 0)
            return null;

        if (isSession)
        {

            var session = windows.FirstOrDefault(static w =>
                w.Period?.TotalHours == 5 ||
                w.Name.Contains("five", StringComparison.OrdinalIgnoreCase) ||
                w.Name.Contains("session", StringComparison.OrdinalIgnoreCase));

            return session ?? windows[0];
        }

        var weekly = windows.FirstOrDefault(static w =>
            w.Period?.TotalDays == 7 ||
            w.Name.Contains("seven", StringComparison.OrdinalIgnoreCase) ||
            w.Name.Contains("week", StringComparison.OrdinalIgnoreCase));

        return weekly ?? (windows.Count > 1 ? windows[1] : null);
    }

    private static string? FormatResetCountdown(DateTimeOffset? resetTimeUtc, DateTimeOffset nowUtc)
    {

        if (!resetTimeUtc.HasValue)
            return null;

        if (resetTimeUtc.Value <= nowUtc)
            return "Resets now";

        var diff = resetTimeUtc.Value - nowUtc;

        if (diff.TotalDays >= 1.0)
            return $"Resets in {(int)diff.TotalDays}d {diff.Hours}h";

        if (diff.TotalHours >= 1.0)
            return $"Resets in {(int)diff.TotalHours}h {diff.Minutes}m";

        return $"Resets in {Math.Max(1, (int)diff.TotalMinutes)}m";
    }

    private static string? ResolveStatusMessage(Snapshot snapshot)
        => snapshot.Status switch
        {
            ProviderStatus.NeedsAuth => !string.IsNullOrWhiteSpace(snapshot.ErrorDescription) ? snapshot.ErrorDescription : "Execute 'claude login' in terminal",
            ProviderStatus.RateLimited => snapshot.ActiveBlock?.Reason ?? snapshot.ErrorDescription ?? "Rate limit reached",
            ProviderStatus.AccessDenied => snapshot.ErrorDescription ?? "Access denied",
            ProviderStatus.Stale => snapshot.ErrorDescription ?? "Telemetry is stale",
            _ => null
        };

    private static string ResolveDefaultName(string providerId)
        => string.Equals(providerId, "claude", StringComparison.OrdinalIgnoreCase)
            ? DEFAULT_CLAUDE_NAME
            : string.Equals(providerId, "gemini", StringComparison.OrdinalIgnoreCase)
                ? "Antigravity"
                : string.Equals(providerId, "codex", StringComparison.OrdinalIgnoreCase)
                    ? "Codex"
                    : string.Equals(providerId, "cursor", StringComparison.OrdinalIgnoreCase)
                        ? "Cursor"
                        : string.Equals(providerId, "mock", StringComparison.OrdinalIgnoreCase)
                            ? DEFAULT_MOCK_NAME
                            : providerId;

    private static string ResolveDefaultBadge(string providerId)
        => string.Equals(providerId, "claude", StringComparison.OrdinalIgnoreCase)
            ? DEFAULT_CLAUDE_BADGE
            : string.Equals(providerId, "gemini", StringComparison.OrdinalIgnoreCase)
                ? "G"
                : string.Equals(providerId, "codex", StringComparison.OrdinalIgnoreCase)
                    ? "X"
                    : string.Equals(providerId, "cursor", StringComparison.OrdinalIgnoreCase)
                        ? "Cu"
                        : string.Equals(providerId, "mock", StringComparison.OrdinalIgnoreCase)
                            ? DEFAULT_MOCK_BADGE
                            : providerId.Length > 0 ? providerId[..1].ToUpperInvariant() : "?";

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {

        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
