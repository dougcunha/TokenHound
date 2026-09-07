using System.Collections.Generic;

namespace TokenHound.Infrastructure.Providers.Copilot;

public sealed partial class CopilotConfigReader
{
    /// <summary>Represents a validated host and login pair from Copilot state.</summary>
    public sealed record LoginIdentity
    {
        /// <summary>Gets the configured GitHub host.</summary>
        public required string Host { get; init; }

        /// <summary>Gets the configured GitHub login.</summary>
        public required string Login { get; init; }
    }

    /// <summary>Represents the subset of Copilot state used for target resolution.</summary>
    public sealed record LoginState
    {
        /// <summary>Gets the most recently used login, if present.</summary>
        public LoginIdentity? LastLoggedInUser { get; init; }

        /// <summary>Gets all explicitly listed logged-in users.</summary>
        public required IReadOnlyList<LoginIdentity> LoggedInUsers { get; init; }
    }
}
