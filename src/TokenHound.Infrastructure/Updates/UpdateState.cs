using System;

namespace TokenHound.Infrastructure.Updates;

/// <summary>
/// Persisted runtime state of the update checker: last completed check and the GitHub rate-limit gate.
/// </summary>
public sealed record UpdateState
{
    /// <summary>
    /// Gets the UTC time of the last completed update check, or <see langword="null"/> when none was recorded.
    /// </summary>
    public DateTimeOffset? LastCheckUtc { get; init; }

    /// <summary>
    /// Gets the UTC deadline before which no update check may be sent, or <see langword="null"/> when not rate limited.
    /// </summary>
    public DateTimeOffset? DeadlineUtc { get; init; }

    /// <summary>
    /// Gets the number of consecutive rate-limit responses since the last successful check.
    /// </summary>
    public int ConsecutiveFailures { get; init; }
}
