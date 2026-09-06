namespace TokenHound.Core.Models;

/// <summary>
/// Represents the execution state of an AI agent session.
/// </summary>
public enum AgentSessionState
{
    /// <summary>
    /// The agent session is active but not currently processing work.
    /// </summary>
    Idle,

    /// <summary>
    /// The agent session is actively processing prompts, tools, or responses.
    /// </summary>
    Busy,

    /// <summary>
    /// The agent session is waiting for user interaction or external input.
    /// </summary>
    Waiting
}
