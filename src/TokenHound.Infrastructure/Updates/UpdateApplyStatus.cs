namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// The result category of an attempt to download and apply a release.
/// </summary>
public enum UpdateApplyStatus
{
    /// <summary>The verified update was handed to the swap or the installer; the app should shut down.</summary>
    Applied,

    /// <summary>The portable folder is not writable; nothing was downloaded.</summary>
    NotWritable,

    /// <summary>The release has no asset for this install mode and architecture; nothing was downloaded.</summary>
    AssetMissing,

    /// <summary>The download failed for a network or file reason; nothing was applied.</summary>
    DownloadFailed,

    /// <summary>The download failed verification (URL, size, or digest); the file was deleted and nothing was applied.</summary>
    IntegrityFailed,

    /// <summary>The swap or the installer launch failed; the previous version is intact.</summary>
    ApplyFailed
}
