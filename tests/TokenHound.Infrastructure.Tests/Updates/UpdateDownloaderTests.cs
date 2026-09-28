using AwesomeAssertions;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies URL restriction, size and digest verification, and cleanup of <see cref="UpdateDownloader"/> (TC-12).
/// </summary>
public sealed class UpdateDownloaderTests : IDisposable
{
    private const string ASSET_NAME = "TokenHound-1.4.0-win-x64-fxdependent.zip";

    private static readonly byte[] PAYLOAD = CreatePayload(200_000);

    private readonly string _directoryPath = Path.Combine(Path.GetTempPath(), $"update_download_test_{Guid.NewGuid():N}");

    /// <inheritdoc />
    public void Dispose()
    {

        if (Directory.Exists(_directoryPath))
            Directory.Delete(_directoryPath, recursive: true);
    }

    /// <summary>
    /// Verifies that a file matching size and digest is kept and reported as digest-verified.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WithMatchingDigest_KeepsVerifiedFile()
    {

        using var downloader = CreateDownloader();

        var result = await downloader.DownloadAsync(Asset(PAYLOAD.Length, Digest(PAYLOAD)), null, TestContext.Current.CancellationToken);

        result.DigestVerified.Should().BeTrue();
        (await File.ReadAllBytesAsync(result.FilePath, TestContext.Current.CancellationToken)).Should().Equal(PAYLOAD);
        Path.GetFileName(result.FilePath).Should().Be(ASSET_NAME);
    }

    /// <summary>
    /// Verifies that without a published digest the size alone is checked.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WithoutDigest_ChecksSizeOnly()
    {

        using var downloader = CreateDownloader();

        var result = await downloader.DownloadAsync(Asset(PAYLOAD.Length, null), null, TestContext.Current.CancellationToken);

        result.DigestVerified.Should().BeFalse();
        File.Exists(result.FilePath).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that a size or digest mismatch is rejected and leaves no file behind.
    /// </summary>
    [Theory]
    [InlineData(1, false, UpdateDownloadFailure.SizeMismatch)]
    [InlineData(0, true, UpdateDownloadFailure.DigestMismatch)]
    public async Task DownloadAsync_WhenVerificationFails_RejectsAndCleansUp(int sizeDelta, bool wrongDigest, UpdateDownloadFailure expected)
    {

        using var downloader = CreateDownloader();
        var digest = wrongDigest ? "sha256:" + new string('0', 64) : null;

        var act = () => downloader.DownloadAsync(Asset(PAYLOAD.Length + sizeDelta, digest), null, TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<UpdateDownloadException>()).Which.Failure.Should().Be(expected);
        Directory.GetFiles(downloader.UpdatesDirectory).Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that a URL outside the repository release downloads is refused before any request.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WithForeignUrl_RefusesWithoutRequest()
    {

        var handler = new ScriptedHttpHandler(_ => Payload());
        using var downloader = new UpdateDownloader(new HttpClient(handler), _directoryPath);
        var asset = Asset(PAYLOAD.Length, null) with { DownloadUrl = new Uri("https://example.com/releases/download/v1.4.0/evil.zip") };

        var act = () => downloader.DownloadAsync(asset, null, TestContext.Current.CancellationToken);

        (await act.Should().ThrowAsync<UpdateDownloadException>()).Which.Failure.Should().Be(UpdateDownloadFailure.InvalidUrl);
        handler.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that cancelling mid-download removes the partial file and propagates the cancellation.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_WhenCancelled_RemovesPartialFile()
    {

        using var downloader = CreateDownloader();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var progress = new CancellingProgress(cancellation);

        var act = () => downloader.DownloadAsync(Asset(PAYLOAD.Length, null), progress, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        Directory.GetFiles(downloader.UpdatesDirectory).Should().BeEmpty();
    }

    private UpdateDownloader CreateDownloader()
        => new(new HttpClient(new ScriptedHttpHandler(_ => Payload())), _directoryPath);

    private static HttpResponseMessage Payload()
        => new(HttpStatusCode.OK) { Content = new ByteArrayContent(PAYLOAD) };

    private static ReleaseAsset Asset(long size, string? digest)
        => new()
        {
            Name = ASSET_NAME,
            Size = size,
            DownloadUrl = new Uri(UpdateDownloader.RELEASE_DOWNLOAD_PREFIX + "v1.4.0/" + ASSET_NAME),
            Digest = digest
        };

    private static string Digest(byte[] bytes)
        => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static byte[] CreatePayload(int length)
    {

        var bytes = new byte[length];
        new Random(42).NextBytes(bytes);

        return bytes;
    }

    private sealed class CancellingProgress(CancellationTokenSource cancellation) : IProgress<double>
    {
        public void Report(double value)
            => cancellation.Cancel();
    }
}
