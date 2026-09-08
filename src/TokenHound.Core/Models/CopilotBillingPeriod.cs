namespace TokenHound.Core.Models;

/// <summary>
/// Describes the requested and verified billing period for Copilot usage.
/// </summary>
public sealed record CopilotBillingPeriod
{
    /// <summary>
    /// Gets the verified start of the period, when known.
    /// </summary>
    public DateTimeOffset? StartUtc { get; init; }

    /// <summary>
    /// Gets the exclusive verified end of the period, when known.
    /// </summary>
    public DateTimeOffset? EndExclusiveUtc { get; init; }

    /// <summary>
    /// Gets the verified reset time, when the source reports one.
    /// </summary>
    public DateTimeOffset? ResetUtc { get; init; }

    /// <summary>
    /// Gets the requested UTC year.
    /// </summary>
    public int RequestedYear { get; init; }

    /// <summary>
    /// Gets the requested UTC month.
    /// </summary>
    public int RequestedMonth { get; init; }

    /// <summary>
    /// Gets a value indicating whether the source verified the period semantics.
    /// </summary>
    public bool IsVerified { get; init; }
}
