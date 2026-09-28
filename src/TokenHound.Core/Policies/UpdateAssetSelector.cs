using System;
using System.Linq;
using System.Runtime.InteropServices;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

/// <summary>
/// Picks the release asset that matches the install mode and process architecture, and parses published digests.
/// </summary>
public static class UpdateAssetSelector
{
    private const string PORTABLE_PREFIX = "TokenHound-";
    private const string SETUP_PREFIX = "TokenHound-Setup-";
    private const string SHA256_PREFIX = "sha256:";
    private const int SHA256_HEX_LENGTH = 64;

    /// <summary>
    /// Maps a process architecture to the release runtime identifier.
    /// </summary>
    /// <param name="architecture">The process architecture.</param>
    /// <returns><c>win-x64</c> or <c>win-arm64</c>, or <see langword="null"/> for architectures without a release build.</returns>
    public static string? ResolveRuntimeIdentifier(Architecture architecture)
        => architecture switch
        {
            Architecture.X64 => "win-x64",
            Architecture.Arm64 => "win-arm64",
            _ => null
        };

    /// <summary>
    /// Selects the asset to download for the install mode and architecture.
    /// </summary>
    /// <param name="release">The release whose assets are searched.</param>
    /// <param name="mode">The install mode of the running copy.</param>
    /// <param name="architecture">The process architecture of the running copy.</param>
    /// <returns>The zip for <see cref="InstallMode.Portable"/> or the setup for <see cref="InstallMode.Installed"/>, or <see langword="null"/>.</returns>
    public static ReleaseAsset? Select(ReleaseInfo release, InstallMode mode, Architecture architecture)
    {

        ArgumentNullException.ThrowIfNull(release);

        var runtimeIdentifier = ResolveRuntimeIdentifier(architecture);

        if (runtimeIdentifier is null)
            return null;

        return mode switch
        {
            InstallMode.Portable => release.Assets.FirstOrDefault(asset => IsPortableAsset(asset.Name, runtimeIdentifier)),
            InstallMode.Installed => release.Assets.FirstOrDefault(asset => IsSetupAsset(asset.Name, runtimeIdentifier)),
            _ => null
        };
    }

    /// <summary>
    /// Parses a GitHub asset digest of the form <c>sha256:&lt;64 hex characters&gt;</c>.
    /// </summary>
    /// <param name="digest">The published digest, or <see langword="null"/>.</param>
    /// <returns>The lowercase hexadecimal SHA-256, or <see langword="null"/> for any other algorithm or a malformed value.</returns>
    public static string? TryParseSha256Digest(string? digest)
    {

        if (digest is null || !digest.StartsWith(SHA256_PREFIX, StringComparison.OrdinalIgnoreCase))
            return null;

        var hex = digest[SHA256_PREFIX.Length..];

        if (hex.Length != SHA256_HEX_LENGTH || !hex.All(char.IsAsciiHexDigit))
            return null;

        return hex.ToLowerInvariant();
    }

    private static bool IsPortableAsset(string name, string runtimeIdentifier)
        => !name.StartsWith(SETUP_PREFIX, StringComparison.OrdinalIgnoreCase)
            && HasShape(name, PORTABLE_PREFIX, $"-{runtimeIdentifier}-fxdependent.zip");

    private static bool IsSetupAsset(string name, string runtimeIdentifier)
        => HasShape(name, SETUP_PREFIX, $"-{runtimeIdentifier}.exe");

    private static bool HasShape(string name, string prefix, string suffix)
        => name.Length > prefix.Length + suffix.Length
            && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}
