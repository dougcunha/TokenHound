using AwesomeAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Updates;
using Xunit;

namespace TokenHound.Infrastructure.Tests.Updates;

/// <summary>
/// Verifies the ordering and failure handling of <see cref="UpdateApplyCoordinator"/> with fake steps (TC-18, apply part).
/// </summary>
public sealed class UpdateApplyCoordinatorTests
{
    private static readonly ReleaseInfo RELEASE = new()
    {
        TagName = "v1.3.0",
        Assets =
        [
            Asset("TokenHound-1.3.0-win-x64-fxdependent.zip"),
            Asset("TokenHound-Setup-1.3.0-win-x64.exe"),
        ],
    };

    private readonly List<string> _calls = [];

    /// <summary>Verifies portable order: writable probe, download, swap, and no installer launch.</summary>
    [Fact]
    public async Task ApplyAsync_Portable_ProbesDownloadsThenSwaps()
    {

        var result = await CreateSut(InstallMode.Portable).ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(UpdateApplyStatus.Applied);
        _calls.Should().Equal("probe", "download:TokenHound-1.3.0-win-x64-fxdependent.zip", "swap:zip");
    }

    /// <summary>Verifies installed order: download then setup launch, with no writable probe or swap.</summary>
    [Fact]
    public async Task ApplyAsync_Installed_DownloadsThenLaunchesSetup()
    {

        var result = await CreateSut(InstallMode.Installed).ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(UpdateApplyStatus.Applied);
        _calls.Should().Equal("download:TokenHound-Setup-1.3.0-win-x64.exe", "launch:setup");
    }

    /// <summary>Verifies that a read-only portable folder fails before any download.</summary>
    [Fact]
    public async Task ApplyAsync_WhenNotWritable_FailsWithoutDownloading()
    {

        var result = await CreateSut(InstallMode.Portable, writable: false).ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(UpdateApplyStatus.NotWritable);
        _calls.Should().Equal("probe");
    }

    /// <summary>Verifies that a missing asset fails before any download.</summary>
    [Fact]
    public async Task ApplyAsync_WhenNoAssetForArchitecture_FailsWithoutDownloading()
    {

        var result = await CreateSut(InstallMode.Portable, architecture: Architecture.Arm64)
            .ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(UpdateApplyStatus.AssetMissing);
        _calls.Should().Equal("probe");
    }

    /// <summary>Verifies that verification and transport failures never reach the applier.</summary>
    [Theory]
    [InlineData(true, UpdateApplyStatus.IntegrityFailed)]
    [InlineData(false, UpdateApplyStatus.DownloadFailed)]
    public async Task ApplyAsync_WhenDownloadFails_NeverApplies(bool integrity, UpdateApplyStatus expected)
    {

        Exception failure = integrity
            ? new UpdateDownloadException(UpdateDownloadFailure.DigestMismatch, "digest")
            : new IOException("disk");

        var result = await CreateSut(InstallMode.Portable, downloadFailure: failure).ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(expected);
        _calls.Should().NotContain(static call => call.StartsWith("swap") || call.StartsWith("launch"));
    }

    /// <summary>Verifies that a failing swap or launch is reported as an apply failure.</summary>
    [Theory]
    [InlineData(InstallMode.Portable)]
    [InlineData(InstallMode.Installed)]
    public async Task ApplyAsync_WhenApplyFails_ReportsApplyFailed(InstallMode mode)
    {

        var result = await CreateSut(mode, applyFailure: new UpdateApplyException("locked")).ApplyAsync(RELEASE, null, CancellationToken.None);

        result.Status.Should().Be(UpdateApplyStatus.ApplyFailed);
        result.Detail.Should().Be("locked");
    }

    /// <summary>Verifies that cancelling the download propagates instead of becoming a failure result.</summary>
    [Fact]
    public async Task ApplyAsync_WhenCancelled_Propagates()
    {

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => CreateSut(InstallMode.Portable, downloadFailure: new OperationCanceledException(cts.Token))
            .ApplyAsync(RELEASE, null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ReleaseAsset Asset(string name)
        => new()
        {
            Name = name,
            Size = 10,
            DownloadUrl = new Uri($"https://github.com/dougcunha/TokenHound/releases/download/v1.3.0/{name}"),
        };

    private UpdateApplyCoordinator CreateSut(
        InstallMode mode,
        bool writable = true,
        Architecture architecture = Architecture.X64,
        Exception? downloadFailure = null,
        Exception? applyFailure = null)
        => new(new UpdateApplySteps
        {
            Mode = mode,
            Architecture = architecture,
            IsWritable = () =>
            {
                _calls.Add("probe");

                return writable;
            },
            DownloadAsync = (asset, _, _) =>
            {
                _calls.Add($"download:{asset.Name}");

                return downloadFailure is null
                    ? Task.FromResult(new DownloadedUpdate { FilePath = "downloaded", Asset = asset, DigestVerified = true })
                    : Task.FromException<DownloadedUpdate>(downloadFailure);
            },
            ApplyPortable = _ =>
            {
                _calls.Add("swap:zip");

                if (applyFailure is not null)
                    throw applyFailure;
            },
            LaunchInstaller = _ =>
            {
                _calls.Add("launch:setup");

                if (applyFailure is not null)
                    throw applyFailure;
            },
        });
}
