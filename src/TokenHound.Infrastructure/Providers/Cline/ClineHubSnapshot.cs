using System;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents the Cline hub daemon advertised by its lock file.
/// </summary>
public sealed record ClineHubSnapshot
{
    /// <summary>
    /// Gets the operating system process identifier of the hub daemon.
    /// </summary>
    public required int Pid { get; init; }

    /// <summary>
    /// Gets the UTC instant when the hub daemon started.
    /// </summary>
    public required DateTimeOffset StartedAtUtc { get; init; }
}