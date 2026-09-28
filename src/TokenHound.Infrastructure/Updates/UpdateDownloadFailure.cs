namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Why a downloaded update was rejected.
/// </summary>
public enum UpdateDownloadFailure
{
    /// <summary>The asset URL is not a release download of the TokenHound repository.</summary>
    InvalidUrl,

    /// <summary>The downloaded length differs from the size in the release metadata.</summary>
    SizeMismatch,

    /// <summary>The SHA-256 of the file differs from the digest published for the asset.</summary>
    DigestMismatch
}
