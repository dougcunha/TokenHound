using System;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

/// <summary>
/// Decides whether a release is an update for the running version and whether a periodic check is due.
/// </summary>
public static class UpdatePolicy
{
    /// <summary>
    /// Evaluates the latest release against the running version and the version the user chose to skip.
    /// </summary>
    /// <param name="current">The running version.</param>
    /// <param name="release">The latest release, or <see langword="null"/> when none is published.</param>
    /// <param name="skippedVersion">The version the user chose to skip, or <see langword="null"/>.</param>
    /// <returns>
    /// <see cref="UpdateCheckStatus.Available"/> only for a newer stable release that is not skipped;
    /// <see cref="UpdateCheckStatus.Unavailable"/> for an unparseable tag; otherwise up to date or skipped.
    /// </returns>
    public static UpdateCheckOutcome Evaluate(ReleaseVersion current, ReleaseInfo? release, string? skippedVersion)
    {

        ArgumentNullException.ThrowIfNull(current);

        if (release is null)
            return new UpdateCheckOutcome { Status = UpdateCheckStatus.UpToDate, Reason = "No published release." };

        if (!ReleaseVersion.TryParse(release.TagName, out var latest))
        {
            return new UpdateCheckOutcome
            {
                Status = UpdateCheckStatus.Unavailable,
                Release = release,
                Reason = $"Release tag '{release.TagName}' is not a version."
            };
        }

        return new UpdateCheckOutcome
        {
            Status = IsNewerStable(release, latest, current)
                ? ResolveSkip(latest, skippedVersion)
                : UpdateCheckStatus.UpToDate,
            Release = release,
            LatestVersion = latest
        };
    }

    /// <summary>
    /// Determines whether a periodic update check is due.
    /// </summary>
    /// <param name="nowUtc">The current UTC time.</param>
    /// <param name="lastCheckUtc">The UTC time of the last completed check, or <see langword="null"/> when none.</param>
    /// <param name="intervalHours">The check interval in hours; zero or less disables periodic checks.</param>
    /// <param name="enabled"><see langword="false"/> when the user disabled automatic checks.</param>
    /// <returns><see langword="true"/> when checks are enabled and the interval elapsed, or no check was recorded.</returns>
    public static bool IsCheckDue(DateTimeOffset nowUtc, DateTimeOffset? lastCheckUtc, int intervalHours, bool enabled)
    {

        if (!enabled || intervalHours <= 0)
            return false;

        if (lastCheckUtc is null || lastCheckUtc.Value > nowUtc)
            return true;

        return nowUtc - lastCheckUtc.Value >= TimeSpan.FromHours(intervalHours);
    }

    private static bool IsNewerStable(ReleaseInfo release, ReleaseVersion latest, ReleaseVersion current)
        => !release.IsDraft && !release.IsPrerelease && !latest.IsPrerelease && latest.IsNewerThan(current);

    private static UpdateCheckStatus ResolveSkip(ReleaseVersion latest, string? skippedVersion)
        => ReleaseVersion.TryParse(skippedVersion, out var skipped) && latest.CompareTo(skipped) == 0
            ? UpdateCheckStatus.Skipped
            : UpdateCheckStatus.Available;
}
