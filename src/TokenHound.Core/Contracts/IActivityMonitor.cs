using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Core.Contracts;

/// <summary>
/// Monitors local process activity and session liveness for an AI coding assistant.
/// </summary>
public interface IActivityMonitor
{
    /// <summary>
    /// Gets the unique identifier of the provider being monitored.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Checks whether an agent process is currently active and running.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>
    /// A <see cref="ValueTask{AgentSession}"/> representing the active session,
    /// or <see langword="null"/> if no active process is detected.
    /// </returns>
    ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default);
}
