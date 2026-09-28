using System;

namespace TokenHound.Core.Models;

/// <summary>
/// The immutable result of one update check.
/// </summary>
public sealed record UpdateCheckOutcome
{
    /// <summary>
    /// Gets the result category.
    /// </summary>
    public required UpdateCheckStatus Status { get; init; }

    /// <summary>
    /// Gets the latest release that was evaluated, when one was returned.
    /// </summary>
    public ReleaseInfo? Release { get; init; }

    /// <summary>
    /// Gets the parsed version of <see cref="Release"/>, when it is a valid version.
    /// </summary>
    public ReleaseVersion? LatestVersion { get; init; }

    /// <summary>
    /// Gets the UTC time after which checks may be sent again, for <see cref="UpdateCheckStatus.RateLimited"/>.
    /// </summary>
    public DateTimeOffset? RetryAfterUtc { get; init; }

    /// <summary>
    /// Gets a diagnostic explanation for non-available outcomes, or <see langword="null"/>.
    /// </summary>
    public string? Reason { get; init; }
}
