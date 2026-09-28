using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Infrastructure.Updates;

namespace TokenHound.App.ViewModels;

/// <summary>
/// The "Update now" path of <see cref="UpdateViewModel"/>: download with progress and Cancel, apply, and the
/// specific error states that keep the app running.
/// </summary>
public sealed partial class UpdateViewModel
{
    private CancellationTokenSource? _downloadCancellation;
    private double _progressPercent;

    /// <summary>Gets a value indicating whether the update is downloading.</summary>
    public bool IsDownloading
        => _state == UpdateDialogState.Downloading;

    /// <summary>Gets a value indicating whether the downloaded update is being applied.</summary>
    public bool IsApplying
        => _state == UpdateDialogState.Applying;

    /// <summary>Gets the download progress from 0 to 100.</summary>
    public double ProgressPercent
        => _progressPercent;

    /// <summary>Gets a value indicating whether an error offers the release page as a manual fallback.</summary>
    public bool ReleaseLinkVisible
        => _state == UpdateDialogState.Error && HasReleaseNotes;

    /// <summary>Starts "Update now" without awaiting it; ignored unless an update is offered and nothing else runs.</summary>
    public void UpdateNow()
        => _ = UpdateNowAsync();

    /// <summary>Downloads and applies the offered release, then requests the application shutdown.</summary>
    /// <returns>A task that completes when the update was handed off, failed, or was cancelled.</returns>
    public async Task UpdateNowAsync()
    {

        if (_isBusy || _disposed || !IsAvailable || _release is null || _dependencies.ApplyAsync is not { } apply)
            return;

        var release = _release;

        SetBusy(true);
        SetProgress(0);
        Show(UpdateDialogState.Downloading, UpdateMessages.Downloading(LatestVersionText), null);

        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _downloadCancellation = cancellation;

        try
        {

            var result = await apply(release, new InlineProgress(OnDownloadProgress), cancellation.Token);
            RenderApplyResult(result);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {

            RestoreOfferAfterCancel();
        }
        catch (Exception ex)
        {

            Log.ForContext<UpdateViewModel>().Error(ex, "Update failed unexpectedly");
            Show(UpdateDialogState.Error, UpdateMessages.APPLY_FAILED, ex.Message);
        }
        finally
        {

            _downloadCancellation = null;
            SetBusy(false);
        }
    }

    /// <summary>Cancels a running download; the dialog returns to the offer and the partial file is removed.</summary>
    public void CancelDownload()
        => _downloadCancellation?.Cancel();

    private void ClearOffer()
    {

        SetOffer(null, null);
        SetProgress(0);
    }

    private void RestoreOfferAfterCancel()
    {

        if (_disposed)
            return;

        SetProgress(0);
        Show(UpdateDialogState.Available, UpdateMessages.Available(LatestVersionText, skipped: false), null);
    }

    private void RenderApplyResult(UpdateApplyResult result)
    {

        var (message, detail) = result.Status switch
        {
            UpdateApplyStatus.Applied => (UpdateMessages.APPLYING, null),
            UpdateApplyStatus.NotWritable => (UpdateMessages.NOT_WRITABLE, null),
            UpdateApplyStatus.AssetMissing => (UpdateMessages.ASSET_MISSING, null),
            UpdateApplyStatus.IntegrityFailed => (UpdateMessages.INTEGRITY_FAILED, result.Detail),
            UpdateApplyStatus.DownloadFailed => (UpdateMessages.DOWNLOAD_FAILED, result.Detail),
            _ => (UpdateMessages.APPLY_FAILED, result.Detail),
        };

        if (result.Status != UpdateApplyStatus.Applied)
        {
            Show(UpdateDialogState.Error, message, detail);

            return;
        }

        Show(UpdateDialogState.Applying, message, null);
        _dependencies.RequestShutdown?.Invoke();
    }

    private void OnDownloadProgress(double fraction)
        => SetProgress(Math.Clamp(fraction, 0, 1) * 100);

    private void SetProgress(double percent)
    {

        _progressPercent = percent;
        OnPropertyChanged(nameof(ProgressPercent));
    }

    private sealed class InlineProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value)
            => report(value);
    }
}
