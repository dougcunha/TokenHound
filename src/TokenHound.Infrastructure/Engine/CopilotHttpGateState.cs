using System;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Represents persisted HTTP rate-limit gate state for Copilot.
/// </summary>
public sealed record CopilotHttpGateState
{
    /// <summary>
    /// Gets the absolute UTC deadline when Copilot requests may resume.
    /// </summary>
    public DateTimeOffset? DeadlineUtc { get; init; }

    /// <summary>
    /// Gets the count of consecutive rate-limit failures encountered.
    /// </summary>
    public int ConsecutiveFailures { get; init; }
}
