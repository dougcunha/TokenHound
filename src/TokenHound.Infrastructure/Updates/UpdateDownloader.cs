using Serilog;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Downloads a release asset into <c>%LOCALAPPDATA%\TokenHound\updates</c> and keeps it only when its size,
/// and its SHA-256 when a digest is published, match the release metadata.
/// </summary>
public sealed class UpdateDownloader : IDisposable
{
    /// <summary>
    /// The only URL prefix accepted for update downloads.
    /// </summary>
    public const string RELEASE_DOWNLOAD_PREFIX = $"https://github.com/{GitHubReleaseClient.REPOSITORY}/releases/download/";

    private const int BUFFER_SIZE = 81920;
    private const string PARTIAL_SUFFIX = ".partial";

    private static readonly ILogger LOGGER = Log.ForContext<UpdateDownloader>();

    private readonly HttpClient _httpClient;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDownloader"/> class.
    /// </summary>
    /// <param name="httpClient">An optional HTTP client; a client without a fixed timeout is created when omitted.</param>
    /// <param name="updatesDirectory">An optional target directory; defaults to <c>%LOCALAPPDATA%\TokenHound\updates</c>.</param>
    public UpdateDownloader(HttpClient? httpClient = null, string? updatesDirectory = null)
    {

        _disposeClient = httpClient is null;
        _httpClient = httpClient ?? new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        UpdatesDirectory = string.IsNullOrWhiteSpace(updatesDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TokenHound", "updates")
            : updatesDirectory;
    }

    /// <summary>
    /// Gets the directory receiving verified downloads.
    /// </summary>
    public string UpdatesDirectory { get; }

    /// <summary>
    /// Downloads and verifies <paramref name="asset"/>.
    /// </summary>
    /// <param name="asset">The release asset to download.</param>
    /// <param name="progress">Optional progress receiver, from 0 to 1.</param>
    /// <param name="cancellationToken">Token cancelling the download; the partial file is removed.</param>
    /// <returns>The verified file.</returns>
    /// <exception cref="UpdateDownloadException">The URL is not a repository release download, or verification failed.</exception>
    public async Task<DownloadedUpdate> DownloadAsync(
        ReleaseAsset asset,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {

        var targetPath = PrepareTarget(asset);
        var partialPath = targetPath + PARTIAL_SUFFIX;

        try
        {

            await CopyToFileAsync(
                asset,
                partialPath,
                progress,
                cancellationToken
            ).ConfigureAwait(false);
            var digestVerified = await VerifyAsync(asset, partialPath, cancellationToken).ConfigureAwait(false);
            File.Move(partialPath, targetPath, true);
            LogCompleted(asset, digestVerified);

            return new DownloadedUpdate { FilePath = targetPath, Asset = asset, DigestVerified = digestVerified };
        }
        catch (Exception ex)
        {

            DeleteIfExists(partialPath);
            LOGGER.Warning(ex, "Update download of {Asset} failed", asset.Name);

            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposeClient)
            _httpClient.Dispose();
    }

    private string PrepareTarget(ReleaseAsset asset)
    {

        ArgumentNullException.ThrowIfNull(asset);
        EnsureAllowedUrl(asset.DownloadUrl);
        Directory.CreateDirectory(UpdatesDirectory);

        return Path.Combine(UpdatesDirectory, Path.GetFileName(asset.Name));
    }

    private static void EnsureAllowedUrl(Uri url)
    {

        if (!url.AbsoluteUri.StartsWith(RELEASE_DOWNLOAD_PREFIX, StringComparison.Ordinal))
            throw new UpdateDownloadException(UpdateDownloadFailure.InvalidUrl, $"Refusing to download an update from '{url}'.");
    }

    private async Task CopyToFileAsync(
        ReleaseAsset asset,
        string path,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {

        using var response = await _httpClient.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? asset.Size;
        var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var sourceScope = source.ConfigureAwait(false);
        var target = CreateTargetFile(path);
        await using var targetScope = target.ConfigureAwait(false);
        var buffer = new byte[BUFFER_SIZE];
        long written = 0;
        int read;

        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            written += read;
            progress?.Report(total > 0 ? Math.Min(1d, (double)written / total) : 0d);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static FileStream CreateTargetFile(string path)
        => new(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BUFFER_SIZE,
            true
        );

    private static async Task<bool> VerifyAsync(ReleaseAsset asset, string path, CancellationToken cancellationToken)
    {

        var length = new FileInfo(path).Length;

        if (length != asset.Size)
            throw new UpdateDownloadException(UpdateDownloadFailure.SizeMismatch, $"Downloaded {length} bytes, expected {asset.Size}.");

        var expected = UpdateAssetSelector.TryParseSha256Digest(asset.Digest);

        if (expected is null)
            return false;

        var stream = File.OpenRead(path);
        await using var streamScope = stream.ConfigureAwait(false);
        var actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));

        if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
            throw new UpdateDownloadException(UpdateDownloadFailure.DigestMismatch, "The downloaded file does not match the published SHA-256 digest.");

        return true;
    }

    private static void LogCompleted(ReleaseAsset asset, bool digestVerified)
        => LOGGER.Information(
            "UpdateDownloadCompleted {Asset} {Bytes} {DigestVerified}",
            asset.Name,
            asset.Size,
            digestVerified
        );

    private static void DeleteIfExists(string path)
    {
        try
        {

            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException ex)
        {

            LOGGER.Warning(ex, "Could not delete partial update file {Path}", path);
        }
    }
}
