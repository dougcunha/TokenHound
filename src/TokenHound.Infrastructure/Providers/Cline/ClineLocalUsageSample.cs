using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Represents the locally sampled Cline session evidence used to build a snapshot.
/// </summary>
public sealed record ClineLocalUsageSample
{
    /// <summary>
    /// Gets the aggregated token usage sampled from the local session artifacts, when any exists.
    /// </summary>
    public ClineLocalUsage? Usage { get; init; }

    /// <summary>
    /// Gets the most recent recorded free model limit failure, when one was found.
    /// </summary>
    public ClineFreeLimitHit? FreeLimit { get; init; }

    /// <summary>
    /// Gets an empty sample carrying no local evidence.
    /// </summary>
    public static ClineLocalUsageSample Empty { get; } = new();
}
