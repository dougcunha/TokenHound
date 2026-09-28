using System;

namespace TokenHound.Core.Models;

/// <summary>
/// A downloadable file attached to a GitHub release.
/// </summary>
public sealed record ReleaseAsset
{
    /// <summary>
    /// Gets the asset file name, for example <c>TokenHound-1.2.3-win-x64-fxdependent.zip</c>.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the asset size in bytes as reported by the release metadata.
    /// </summary>
    public required long Size { get; init; }

    /// <summary>
    /// Gets the public download URL of the asset.
    /// </summary>
    public required Uri DownloadUrl { get; init; }

    /// <summary>
    /// Gets the digest published for the asset (for example <c>sha256:&lt;hex&gt;</c>), or <see langword="null"/> when absent.
    /// </summary>
    public string? Digest { get; init; }
}
