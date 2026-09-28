using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Drives the update dialog: runs a check through a delegate, renders one state per outcome, and handles
/// Later, Skip this version, Update now, and the release-notes link.
/// </summary>
public sealed partial class UpdateViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly ILogger LOGGER = Log.ForContext<UpdateViewModel>();

    private readonly UpdateDialogDependencies _dependencies;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationTokenSource _lifetime = new();

    private UpdateDialogState _state = UpdateDialogState.Checking;
    private string _message = UpdateMessages.CHECKING;
    private string? _detail;
    private ReleaseInfo? _release;
    private ReleaseVersion? _latestVersion;
    private bool _isBusy;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="UpdateViewModel"/> class.</summary>
    /// <param name="dependencies">The check, persistence, and navigation delegates.</param>
    /// <param name="timeProvider">The clock whose local zone renders the resume time; defaults to the system clock.</param>
    public UpdateViewModel(UpdateDialogDependencies dependencies, TimeProvider? timeProvider = null)
    {

        ArgumentNullException.ThrowIfNull(dependencies);

        _dependencies = dependencies;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised when the dialog should close (Later, Close, or a saved skip).</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Gets the current dialog state.</summary>
    public UpdateDialogState State
        => _state;

    /// <summary>Gets the headline message for the current state.</summary>
    public string Message
        => _message;

    /// <summary>Gets the diagnostic detail for an error, or <see langword="null"/>.</summary>
    public string? Detail
        => _detail;

    /// <summary>Gets a value indicating whether <see cref="Detail"/> has text.</summary>
    public bool HasDetail
        => !string.IsNullOrWhiteSpace(_detail);

    /// <summary>Gets a value indicating whether a check or a save is running.</summary>
    public bool IsBusy
        => _isBusy;

    /// <summary>Gets a value indicating whether a check is running.</summary>
    public bool IsChecking
        => _state == UpdateDialogState.Checking;

    /// <summary>Gets a value indicating whether a newer release is offered.</summary>
    public bool IsAvailable
        => _state == UpdateDialogState.Available;

    /// <summary>Gets a value indicating whether the state is a final result without an offer.</summary>
    public bool IsResult
        => _state is UpdateDialogState.UpToDate or UpdateDialogState.RateLimited or UpdateDialogState.Error;

    /// <summary>Gets a value indicating whether the user may retry the check.</summary>
    public bool CanRetry
        => _state == UpdateDialogState.Error;

    /// <summary>Gets the running version text.</summary>
    public string CurrentVersionText
        => _dependencies.CurrentVersion?.ToString() ?? UpdateMessages.UNKNOWN_VERSION;

    /// <summary>Gets the offered version text, or an empty string.</summary>
    public string LatestVersionText
        => _latestVersion?.ToString() ?? string.Empty;

    /// <summary>Gets a value indicating whether the offered release has a release-notes page.</summary>
    public bool HasReleaseNotes
        => _release?.HtmlUrl is not null;

    /// <summary>Starts a check without awaiting it; ignored while another check or save runs.</summary>
    public void StartCheck()
        => _ = CheckAsync();

    /// <summary>Runs a check and renders its outcome; ignored while another check or save runs.</summary>
    /// <returns>A task that completes when the outcome is rendered.</returns>
    public async Task CheckAsync()
    {

        if (_isBusy || _disposed)
            return;

        SetBusy(true);
        ClearOffer();
        Show(UpdateDialogState.Checking, UpdateMessages.CHECKING, null);

        try
        {

            var outcome = await _dependencies.CheckAsync(_lifetime.Token);
            Render(outcome);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {

            LOGGER.Debug("Update check abandoned because the dialog closed");
        }
        catch (Exception ex)
        {

            LOGGER.Error(ex, "Update check failed unexpectedly");
            Show(UpdateDialogState.Error, UpdateMessages.CHECK_FAILED, ex.Message);
        }
        finally
        {

            SetBusy(false);
        }
    }

    /// <summary>Renders an already known check outcome, such as the one behind a balloon; ignored while busy.</summary>
    /// <param name="outcome">The outcome to render.</param>
    public void ShowOutcome(UpdateCheckOutcome outcome)
    {

        ArgumentNullException.ThrowIfNull(outcome);

        if (_isBusy || _disposed)
            return;

        Render(outcome);
    }

    /// <summary>Closes the dialog without persisting anything; the next periodic check asks again.</summary>
    public void Later()
        => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Persists the offered version as skipped and closes; keeps the dialog open with an error when saving fails.</summary>
    /// <returns>A task that completes when the close is requested or the error is shown.</returns>
    public async Task SkipAsync()
    {

        if (_isBusy || _disposed || !IsAvailable || _latestVersion is null)
            return;

        SetBusy(true);

        try
        {

            await SaveSkipAsync(_latestVersion.ToString());
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {

            LOGGER.Debug("Skip abandoned because the dialog closed");
        }
        catch (Exception ex)
        {

            LOGGER.Error(ex, "Saving the skipped update version failed");
            Show(UpdateDialogState.Error, UpdateMessages.SKIP_FAILED, ex.Message);
        }
        finally
        {

            SetBusy(false);
        }
    }

    /// <summary>Opens the release-notes page of the offered release.</summary>
    public void OpenReleaseNotes()
    {

        if (_release?.HtmlUrl is { } url)
            _dependencies.OpenUrl?.Invoke(url);
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task SaveSkipAsync(string version)
    {

        if (!await _dependencies.SaveSkippedVersionAsync(version, _lifetime.Token))
        {
            Show(UpdateDialogState.Error, UpdateMessages.SKIP_FAILED, null);

            return;
        }

        LOGGER.Information("Update {Version} skipped by the user", version);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Render(UpdateCheckOutcome outcome)
    {

        switch (outcome.Status)
        {
            case UpdateCheckStatus.Available or UpdateCheckStatus.Skipped:
                SetOffer(outcome.Release, outcome.LatestVersion);
                var skipped = outcome.Status == UpdateCheckStatus.Skipped;
                Show(UpdateDialogState.Available, UpdateMessages.Available(LatestVersionText, skipped), null);
                break;

            case UpdateCheckStatus.UpToDate:
                Show(UpdateDialogState.UpToDate, UpdateMessages.UpToDate(CurrentVersionText), null);
                break;

            case UpdateCheckStatus.RateLimited:
                var message = UpdateMessages.RateLimited(outcome.RetryAfterUtc, _timeProvider);
                Show(UpdateDialogState.RateLimited, message, null);
                break;

            case UpdateCheckStatus.Unavailable:
                Show(UpdateDialogState.Error, UpdateMessages.UNAVAILABLE, outcome.Reason);
                break;

            default:
                Show(UpdateDialogState.Error, UpdateMessages.CHECK_FAILED, outcome.Reason);
                break;
        }
    }

    private void SetOffer(ReleaseInfo? release, ReleaseVersion? latestVersion)
    {

        _release = release;
        _latestVersion = latestVersion;
        OnPropertyChanged(nameof(LatestVersionText));
        OnPropertyChanged(nameof(HasReleaseNotes));
    }

    private void Show(UpdateDialogState state, string message, string? detail)
    {

        _state = state;
        _message = message;
        _detail = detail;

        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(Detail));
        OnPropertyChanged(nameof(HasDetail));
        OnPropertyChanged(nameof(IsChecking));
        OnPropertyChanged(nameof(IsAvailable));
        OnPropertyChanged(nameof(IsResult));
        OnPropertyChanged(nameof(CanRetry));
        OnPropertyChanged(nameof(IsDownloading));
        OnPropertyChanged(nameof(IsApplying));
        OnPropertyChanged(nameof(ReleaseLinkVisible));
    }

    private void SetBusy(bool value)
    {

        _isBusy = value;
        OnPropertyChanged(nameof(IsBusy));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
