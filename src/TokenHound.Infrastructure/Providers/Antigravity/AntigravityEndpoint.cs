using System.Collections.Generic;

namespace TokenHound.Infrastructure.Providers.Antigravity;

/// <summary>
/// Represents a discovered local Language Server endpoint with its process ID, CSRF token, and candidate ports.
/// </summary>
public sealed record AntigravityEndpoint
{
    /// <summary>
    /// Gets the process ID of the running Language Server.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Gets the CSRF token extracted from the process arguments.
    /// </summary>
    public required string CsrfToken { get; init; }

    /// <summary>
    /// Gets the candidate listening TCP ports associated with this process.
    /// </summary>
    public required IReadOnlyList<int> CandidatePorts { get; init; }
}
