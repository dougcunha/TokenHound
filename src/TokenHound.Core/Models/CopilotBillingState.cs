namespace TokenHound.Core.Models;

/// <summary>
/// Describes the freshness state of a Copilot billing reading.
/// </summary>
public enum CopilotBillingState
{
    /// <summary>
    /// The reading was obtained successfully for the current attempt.
    /// </summary>
    Available,

    /// <summary>
    /// The reading is retained from an earlier successful attempt.
    /// </summary>
    Stale,

    /// <summary>
    /// No usable billing reading is available.
    /// </summary>
    Unavailable
}
