namespace TokenHound.Core.Models;

/// <summary>
/// Represents an active process session associated with an AI coding agent.
/// </summary>
public sealed record AgentSession
{
    /// <summary>
    /// Gets the operating system process identifier (PID) of the agent.
    /// </summary>
    public required int Pid { get; init; }

    /// <summary>
    /// Gets the timestamp when the agent process was launched.
    /// </summary>
    public required DateTimeOffset StartTimeUtc { get; init; }

    /// <summary>
    /// Gets the current operational state of the agent session.
    /// </summary>
    public required AgentSessionState State { get; init; }

    /// <summary>
    /// Gets the timestamp of the most recent recorded activity from the agent.
    /// </summary>
    public required DateTimeOffset LastActivityUtc { get; init; }
}
