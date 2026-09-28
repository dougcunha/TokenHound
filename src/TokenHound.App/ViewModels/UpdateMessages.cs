using System;
using System.Globalization;

namespace TokenHound.App.ViewModels;

/// <summary>
/// Copy shown by the update dialog, one message per check outcome.
/// </summary>
public static class UpdateMessages
{
    /// <summary>Headline while a check runs.</summary>
    public const string CHECKING = "Checking for updates…";

    /// <summary>Headline format when the running version is current; {0} is the running version.</summary>
    public const string UP_TO_DATE_FORMAT = "TokenHound {0} is up to date.";

    /// <summary>Headline format when a newer release exists; {0} is the new version.</summary>
    public const string AVAILABLE_FORMAT = "TokenHound {0} is available.";

    /// <summary>Headline format when the newer release was skipped earlier; {0} is the new version.</summary>
    public const string SKIPPED_FORMAT = "TokenHound {0} is available. You chose to skip this version earlier.";

    /// <summary>Headline format when GitHub rate-limited checks; {0} is the local resume time.</summary>
    public const string RATE_LIMITED_FORMAT = "GitHub limited update checks. Checks resume at {0}.";

    /// <summary>Headline when GitHub rate-limited checks without a known resume time.</summary>
    public const string RATE_LIMITED = "GitHub limited update checks. Try again later.";

    /// <summary>Headline when the check failed (offline, timeout, service error).</summary>
    public const string CHECK_FAILED = "Could not check for updates. Check your connection and try again.";

    /// <summary>Headline when this build cannot be compared with releases.</summary>
    public const string UNAVAILABLE = "Update checks are unavailable for this build.";

    /// <summary>Headline when the skipped version could not be saved.</summary>
    public const string SKIP_FAILED = "Could not save the skipped version. Try again.";

    /// <summary>Headline format while the update downloads; {0} is the new version.</summary>
    public const string DOWNLOADING_FORMAT = "Downloading TokenHound {0}…";

    /// <summary>Headline while the downloaded update is applied and the app restarts.</summary>
    public const string APPLYING = "Installing the update. TokenHound will restart.";

    /// <summary>Headline when the portable folder cannot be written; the release page offers a manual update.</summary>
    public const string NOT_WRITABLE = "TokenHound cannot update itself because its folder is read-only. Download the update from the release page.";

    /// <summary>Headline when the release has no download for this build.</summary>
    public const string ASSET_MISSING = "This release has no download for your build. Get it from the release page.";

    /// <summary>Headline when the download failed verification.</summary>
    public const string INTEGRITY_FAILED = "The downloaded update failed verification and was discarded. Nothing was changed.";

    /// <summary>Headline when the download failed.</summary>
    public const string DOWNLOAD_FAILED = "The update could not be downloaded. Check your connection and try again.";

    /// <summary>Headline when applying the update failed.</summary>
    public const string APPLY_FAILED = "The update could not be installed. The current version was kept.";

    /// <summary>Running version text when the version is not a release version.</summary>
    public const string UNKNOWN_VERSION = "unknown";

    /// <summary>Formats the up-to-date headline.</summary>
    /// <param name="currentVersion">The running version text.</param>
    /// <returns>The headline.</returns>
    public static string UpToDate(string currentVersion)
        => string.Format(CultureInfo.CurrentCulture, UP_TO_DATE_FORMAT, currentVersion);

    /// <summary>Formats the downloading headline.</summary>
    /// <param name="latestVersion">The offered version text.</param>
    /// <returns>The headline.</returns>
    public static string Downloading(string latestVersion)
        => string.Format(CultureInfo.CurrentCulture, DOWNLOADING_FORMAT, latestVersion);

    /// <summary>Formats the update-available headline.</summary>
    /// <param name="latestVersion">The offered version text.</param>
    /// <param name="skipped"><see langword="true"/> when the user skipped this version earlier.</param>
    /// <returns>The headline.</returns>
    public static string Available(string latestVersion, bool skipped)
        => string.Format(CultureInfo.CurrentCulture, skipped ? SKIPPED_FORMAT : AVAILABLE_FORMAT, latestVersion);

    /// <summary>Formats the rate-limited headline with the resume time in the clock's local zone.</summary>
    /// <param name="retryAfterUtc">The UTC resume time, or <see langword="null"/> when unknown.</param>
    /// <param name="timeProvider">The clock supplying the current time and local zone.</param>
    /// <returns>The headline; the date is included when the resume time is not today.</returns>
    public static string RateLimited(DateTimeOffset? retryAfterUtc, TimeProvider timeProvider)
    {

        ArgumentNullException.ThrowIfNull(timeProvider);

        if (retryAfterUtc is not { } deadline)
            return RATE_LIMITED;

        var local = TimeZoneInfo.ConvertTime(deadline, timeProvider.LocalTimeZone);
        var today = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeProvider.LocalTimeZone).Date;
        var pattern = local.Date == today ? "HH:mm" : "yyyy-MM-dd HH:mm";

        return string.Format(
            CultureInfo.CurrentCulture,
            RATE_LIMITED_FORMAT,
            local.ToString(pattern, CultureInfo.InvariantCulture)
        );
    }
}
