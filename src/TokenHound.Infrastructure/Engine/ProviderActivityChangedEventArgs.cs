using System;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

/// <summary>
/// Carries an activity state change from a provider activity monitor.
/// </summary>
public sealed class ProviderActivityChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderActivityChangedEventArgs"/> class.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="agentSession">The current session, or null when the provider is idle.</param>
    public ProviderActivityChangedEventArgs(
        string providerId,
        AgentSession? agentSession)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ProviderId = providerId;
        AgentSession = agentSession;
    }

    /// <summary>
    /// Gets the provider identifier associated with the activity state.
    /// </summary>
    public string ProviderId { get; }

    /// <summary>
    /// Gets the current session, or null when the provider is idle.
    /// </summary>
    public AgentSession? AgentSession { get; }
}
