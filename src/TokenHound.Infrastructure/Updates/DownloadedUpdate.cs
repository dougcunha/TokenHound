using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// A verified update file ready to be applied.
/// </summary>
public sealed record DownloadedUpdate
{
    /// <summary>
    /// Gets the full path of the verified file.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the release asset the file was downloaded from.
    /// </summary>
    public required ReleaseAsset Asset { get; init; }

    /// <summary>
    /// Gets a value indicating whether a published SHA-256 digest was checked (otherwise only the size was).
    /// </summary>
    public required bool DigestVerified { get; init; }
}
