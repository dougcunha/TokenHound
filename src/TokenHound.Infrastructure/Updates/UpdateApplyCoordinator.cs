using Serilog;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Runs "Update now": probe writability (portable), select the asset, download and verify, then apply.
/// Nothing is applied before verification, and every failure is reported as a result so the app keeps running.
/// </summary>
public sealed class UpdateApplyCoordinator
{
    private static readonly ILogger LOGGER = Log.ForContext<UpdateApplyCoordinator>();

    private readonly UpdateApplySteps _steps;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateApplyCoordinator"/> class.
    /// </summary>
    /// <param name="steps">The environment facts and side effects to sequence.</param>
    public UpdateApplyCoordinator(UpdateApplySteps steps)
    {

        ArgumentNullException.ThrowIfNull(steps);

        _steps = steps;
    }

    /// <summary>
    /// Downloads, verifies, and applies <paramref name="release"/>.
    /// </summary>
    /// <param name="release">The release to install.</param>
    /// <param name="progress">Optional download progress receiver, from 0 to 1.</param>
    /// <param name="cancellationToken">Token cancelling the download; cancellation propagates as an exception.</param>
    /// <returns>The result; <see cref="UpdateApplyStatus.Applied"/> means the app should shut down now.</returns>
    public async Task<UpdateApplyResult> ApplyAsync(
        ReleaseInfo release,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {

        ArgumentNullException.ThrowIfNull(release);

        if (_steps.Mode == InstallMode.Portable && !_steps.IsWritable())
            return Fail(UpdateApplyStatus.NotWritable, "The application folder is not writable.");

        var asset = UpdateAssetSelector.Select(release, _steps.Mode, _steps.Architecture);

        if (asset is null)
            return Fail(UpdateApplyStatus.AssetMissing, $"No {_steps.Mode} asset for {_steps.Architecture} in {release.TagName}.");

        var (download, failure) = await DownloadAsync(asset, progress, cancellationToken).ConfigureAwait(false);

        return download is null ? failure! : await ApplyDownloadAsync(download, cancellationToken).ConfigureAwait(false);
    }

    private async Task<(DownloadedUpdate? Download, UpdateApplyResult? Failure)> DownloadAsync(
        ReleaseAsset asset,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {

        try
        {

            return (await _steps.DownloadAsync(asset, progress, cancellationToken).ConfigureAwait(false), null);
        }
        catch (UpdateDownloadException ex) when (ex.Failure is not null)
        {

            return (null, Fail(UpdateApplyStatus.IntegrityFailed, ex.Message));
        }
        catch (Exception ex) when (ex is UpdateDownloadException or HttpRequestException or IOException)
        {

            return (null, Fail(UpdateApplyStatus.DownloadFailed, ex.Message));
        }
    }

    private async Task<UpdateApplyResult> ApplyDownloadAsync(DownloadedUpdate download, CancellationToken cancellationToken)
    {

        try
        {

            if (_steps.Mode == InstallMode.Portable)
                await Task.Run(() => _steps.ApplyPortable(download.FilePath), cancellationToken).ConfigureAwait(false);
            else
                _steps.LaunchInstaller(download.FilePath);

            return new UpdateApplyResult { Status = UpdateApplyStatus.Applied };
        }
        catch (Exception ex) when (ex is UpdateApplyException or IOException or Win32Exception or InvalidOperationException)
        {

            LOGGER.Error(ex, "UpdateApplyFailed {Mode} {Error}", _steps.Mode, ex.Message);

            return new UpdateApplyResult { Status = UpdateApplyStatus.ApplyFailed, Detail = ex.Message };
        }
    }

    private UpdateApplyResult Fail(UpdateApplyStatus status, string detail)
    {

        LOGGER.Warning("UpdateApplyFailed {Mode} {Error}", _steps.Mode, detail);

        return new UpdateApplyResult { Status = status, Detail = detail };
    }
}
