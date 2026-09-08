namespace TokenHound.Core.Models;

/// <summary>
/// Represents independent Copilot billing state attached to a provider snapshot.
/// </summary>
public sealed record CopilotBillingStatus
{
    /// <summary>
    /// Gets the freshness state of the billing reading.
    /// </summary>
    public required CopilotBillingState State { get; init; }

    /// <summary>
    /// Gets the reason for an unavailable or stale billing reading.
    /// </summary>
    public required CopilotBillingReason Reason { get; init; }

    /// <summary>
    /// Gets the billing usage, when one was obtained or retained.
    /// </summary>
    public CopilotCreditUsage? Usage { get; init; }

    /// <summary>
    /// Gets the time when the billing attempt was made.
    /// </summary>
    public required DateTimeOffset AttemptedAtUtc { get; init; }

    /// <summary>
    /// Gets the next time a billing request may be attempted, when rate limited.
    /// </summary>
    public DateTimeOffset? NextRequestAtUtc { get; init; }
}
