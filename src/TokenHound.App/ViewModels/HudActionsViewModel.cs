using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Coordinates presentation state and execution for HUD context menu actions.
/// </summary>
public sealed class HudActionsViewModel : INotifyPropertyChanged
{
    private const string REFRESHING_TEXT = "Refreshing usage...";
    private const string COMPLETED_TEXT = "Refresh completed";
    private const string NO_PROVIDERS_TEXT = "No providers available";
    private const string NO_COUNTERS_TEXT = "No counters updated. Check provider status.";
    private const string ATTENTION_TEXT = "Refresh completed. Some providers need attention.";
    private const string FAILED_TEXT = "Refresh failed.";

    private readonly Func<CancellationToken, Task> _refreshAction;
    private readonly Func<Task> _shutdownAction;
    private readonly Action _showSettings;
    private readonly Action _showAbout;
    private readonly Func<IEnumerable<Snapshot>>? _snapshotsProvider;

    private readonly object _syncLock = new();
    private Task? _shutdownTask;
    private bool _isRefreshing;
    private bool _isShuttingDown;
    private string? _refreshStatusText;

    /// <summary>
    /// Initializes a new instance of the <see cref="HudActionsViewModel"/> class.
    /// </summary>
    /// <param name="refreshAction">The asynchronous refresh operation.</param>
    /// <param name="shutdownAction">The asynchronous coordinated-shutdown operation, invoked only by the tray "Exit" action.</param>
    /// <param name="showSettings">Action to show the modeless Settings dialog.</param>
    /// <param name="showAbout">Action to show the modeless About dialog.</param>
    /// <param name="snapshotsProvider">Optional provider to query current snapshots for status formulation.</param>
    public HudActionsViewModel(
        Func<CancellationToken, Task> refreshAction,
        Func<Task> shutdownAction,
        Action showSettings,
        Action showAbout,
        Func<IEnumerable<Snapshot>>? snapshotsProvider = null)
    {

        ArgumentNullException.ThrowIfNull(refreshAction);
        ArgumentNullException.ThrowIfNull(shutdownAction);
        ArgumentNullException.ThrowIfNull(showSettings);
        ArgumentNullException.ThrowIfNull(showAbout);

        _refreshAction = refreshAction;
        _shutdownAction = shutdownAction;
        _showSettings = showSettings;
        _showAbout = showAbout;
        _snapshotsProvider = snapshotsProvider;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets a value indicating whether an asynchronous refresh operation is currently in progress.
    /// </summary>
    public bool IsRefreshing
        => _isRefreshing;

    /// <summary>
    /// Gets a value indicating whether the coordinated application shutdown has been initiated.
    /// </summary>
    public bool IsShuttingDown
        => _isShuttingDown;

    /// <summary>
    /// Gets the current status feedback text, or <see langword="null"/> if no feedback is active.
    /// </summary>
    public string? RefreshStatusText
        => _refreshStatusText;

    /// <summary>
    /// Gets a value indicating whether status feedback should be displayed.
    /// </summary>
    public bool IsStatusVisible
        => !string.IsNullOrWhiteSpace(_refreshStatusText);

    /// <summary>
    /// Dismisses any active status feedback.
    /// </summary>
    public void DismissStatus()
    {

        if (_refreshStatusText is null)
            return;

        _refreshStatusText = null;
        OnPropertyChanged(nameof(RefreshStatusText));
        OnPropertyChanged(nameof(IsStatusVisible));
    }

    /// <summary>
    /// Initiates an immediate refresh of all providers, guarding against concurrent or post-close execution.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A task representing the refresh operation.</returns>
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {

        lock (_syncLock)
        {

            if (_isRefreshing || _isShuttingDown)
                return;

            _isRefreshing = true;
        }

        OnPropertyChanged(nameof(IsRefreshing));
        SetStatusText(REFRESHING_TEXT);

        try
        {

            await _refreshAction(cancellationToken);

            if (!_isShuttingDown)
            {

                var snapshots = _snapshotsProvider?.Invoke();
                SetStatusText(FormulateStatusText(snapshots));
            }
        }
        catch (OperationCanceledException) when (_isShuttingDown || cancellationToken.IsCancellationRequested)
        {

            SetStatusText(null);
        }
        catch (Exception)
        {

            if (!_isShuttingDown)
                SetStatusText(FAILED_TEXT);
        }
        finally
        {

            if (!_isShuttingDown)
            {

                _isRefreshing = false;
                OnPropertyChanged(nameof(IsRefreshing));
            }
        }
    }

    /// <summary>
    /// Initiates the coordinated application shutdown, preventing any further action dispatch.
    /// </summary>
    /// <returns>A shared task representing the terminal shutdown operation.</returns>
    public Task ShutdownAsync()
    {

        lock (_syncLock)
        {

            if (_shutdownTask is not null)
                return _shutdownTask;

            _isShuttingDown = true;
            _shutdownTask = _shutdownAction();
        }

        SetStatusText(null);
        OnPropertyChanged(nameof(IsShuttingDown));

        return _shutdownTask;
    }

    /// <summary>
    /// Shows the Settings dialog if the application is not closing.
    /// </summary>
    public void ShowSettings()
    {

        if (_isShuttingDown)
            return;

        _showSettings();
    }

    /// <summary>
    /// Shows the About dialog if the application is not closing.
    /// </summary>
    public void ShowAbout()
    {

        if (_isShuttingDown)
            return;

        _showAbout();
    }

    /// <summary>
    /// Formulates summary status text based on the collection of current provider snapshots.
    /// </summary>
    /// <param name="snapshots">The collection of current snapshots.</param>
    /// <returns>The formulated status description.</returns>
    public static string FormulateStatusText(IEnumerable<Snapshot>? snapshots)
    {

        if (snapshots is null)
            return NO_PROVIDERS_TEXT;

        var list = snapshots as IReadOnlyCollection<Snapshot> ?? snapshots.ToList();

        if (list.Count == 0)
            return NO_PROVIDERS_TEXT;

        var okCount = list.Count(static s => s.Status == ProviderStatus.Ok);

        if (okCount == list.Count)
            return COMPLETED_TEXT;

        if (okCount == 0)
            return NO_COUNTERS_TEXT;

        return ATTENTION_TEXT;
    }

    private void SetStatusText(string? text)
    {

        if (string.Equals(_refreshStatusText, text, StringComparison.Ordinal))
            return;

        _refreshStatusText = text;
        OnPropertyChanged(nameof(RefreshStatusText));
        OnPropertyChanged(nameof(IsStatusVisible));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
