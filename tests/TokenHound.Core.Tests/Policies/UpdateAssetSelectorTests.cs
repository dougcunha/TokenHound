using AwesomeAssertions;
using System;
using System.Runtime.InteropServices;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Core.Tests.Policies;

/// <summary>
/// Verifies asset selection and digest parsing in <see cref="UpdateAssetSelector"/> (TC-05, TC-06).
/// </summary>
public sealed class UpdateAssetSelectorTests
{
    private static readonly ReleaseInfo RELEASE = new()
    {
        TagName = "v1.4.0",
        Assets =
        [
            Asset("TokenHound-1.4.0-win-x64-fxdependent.zip"),
            Asset("TokenHound-1.4.0-win-arm64-fxdependent.zip"),
            Asset("TokenHound-Setup-1.4.0-win-x64.exe"),
            Asset("TokenHound-Setup-1.4.0-win-arm64.exe")
        ]
    };

    /// <summary>
    /// Verifies that each install mode and architecture maps to its asset.
    /// </summary>
    [Theory]
    [InlineData(InstallMode.Portable, Architecture.X64, "TokenHound-1.4.0-win-x64-fxdependent.zip")]
    [InlineData(InstallMode.Portable, Architecture.Arm64, "TokenHound-1.4.0-win-arm64-fxdependent.zip")]
    [InlineData(InstallMode.Installed, Architecture.X64, "TokenHound-Setup-1.4.0-win-x64.exe")]
    [InlineData(InstallMode.Installed, Architecture.Arm64, "TokenHound-Setup-1.4.0-win-arm64.exe")]
    public void Select_ReturnsAssetForModeAndArchitecture(InstallMode mode, Architecture architecture, string expected)
    {
        var asset = UpdateAssetSelector.Select(RELEASE, mode, architecture);

        asset!.Name.Should().Be(expected);
    }

    /// <summary>
    /// Verifies that names are matched case-insensitively.
    /// </summary>
    [Fact]
    public void Select_IgnoresNameCase()
    {
        var release = new ReleaseInfo { TagName = "v1.4.0", Assets = [Asset("tokenhound-setup-1.4.0-WIN-X64.EXE")] };

        UpdateAssetSelector.Select(release, InstallMode.Installed, Architecture.X64).Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that no asset is returned for an unsupported architecture or a missing name.
    /// </summary>
    [Theory]
    [InlineData(InstallMode.Portable, Architecture.X86)]
    [InlineData(InstallMode.Installed, Architecture.Arm)]
    public void Select_WithUnsupportedArchitecture_ReturnsNull(InstallMode mode, Architecture architecture)
    {
        UpdateAssetSelector.Select(RELEASE, mode, architecture).Should().BeNull();
    }

    /// <summary>
    /// Verifies that a release lacking the expected asset yields no selection, and the setup is never taken as the zip.
    /// </summary>
    [Fact]
    public void Select_WhenAssetMissing_ReturnsNull()
    {
        var release = new ReleaseInfo
        {
            TagName = "v1.4.0",
            Assets = [Asset("TokenHound-Setup-1.4.0-win-x64.exe"), Asset("TokenHound--win-x64-fxdependent.zip")]
        };

        UpdateAssetSelector.Select(release, InstallMode.Portable, Architecture.X64).Should().BeNull();
        UpdateAssetSelector.Select(release, InstallMode.Installed, Architecture.Arm64).Should().BeNull();
    }

    /// <summary>
    /// Verifies SHA-256 digest parsing and rejection of other algorithms or malformed values.
    /// </summary>
    [Theory]
    [InlineData("sha256:ABCDEF0123456789abcdef0123456789ABCDEF0123456789abcdef0123456789", "abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789")]
    [InlineData("SHA256:0000000000000000000000000000000000000000000000000000000000000000", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("sha512:abcdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789", null)]
    [InlineData("sha256:abc", null)]
    [InlineData("sha256:zzzdef0123456789abcdef0123456789abcdef0123456789abcdef0123456789", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void TryParseSha256Digest_ReturnsLowercaseHexOrNull(string? digest, string? expected)
    {
        UpdateAssetSelector.TryParseSha256Digest(digest).Should().Be(expected);
    }

    private static ReleaseAsset Asset(string name)
        => new() { Name = name, Size = 1, DownloadUrl = new Uri($"https://github.com/dougcunha/TokenHound/releases/download/v1.4.0/{name}") };
}
