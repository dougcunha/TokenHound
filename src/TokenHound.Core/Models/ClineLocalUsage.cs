using System;

namespace TokenHound.Core.Models;

/// <summary>
/// Represents token usage aggregated from the local session artifacts written by the Cline CLI.
/// </summary>
/// <remarks>
/// Cline publishes no authoritative token quota, so these totals are locally derived evidence and
/// never carry a utilization fraction.
/// </remarks>
public sealed record ClineLocalUsage
{
    /// <summary>
    /// Gets the aggregated prompt (input) token count recorded within the sampled window.
    /// </summary>
    public required long InputTokens { get; init; }

    /// <summary>
    /// Gets the aggregated completion (output) token count recorded within the sampled window.
    /// </summary>
    public required long OutputTokens { get; init; }

    /// <summary>
    /// Gets the aggregated prompt-cache read token count recorded within the sampled window.
    /// </summary>
    public required long CacheReadTokens { get; init; }

    /// <summary>
    /// Gets the aggregated prompt-cache write token count recorded within the sampled window.
    /// </summary>
    public required long CacheWriteTokens { get; init; }

    /// <summary>
    /// Gets the number of model calls that contributed to the aggregated totals.
    /// </summary>
    public required int ModelCalls { get; init; }

    /// <summary>
    /// Gets the UTC start of the sampled aggregation window.
    /// </summary>
    public required DateTimeOffset WindowStartUtc { get; init; }

    /// <summary>
    /// Gets the UTC timestamp of the most recent recorded model call.
    /// </summary>
    public required DateTimeOffset LastActivityUtc { get; init; }

    /// <summary>
    /// Gets the total number of tokens across input, output, and cache buckets.
    /// </summary>
    public long TotalTokens
        => InputTokens + OutputTokens + CacheReadTokens + CacheWriteTokens;
}
