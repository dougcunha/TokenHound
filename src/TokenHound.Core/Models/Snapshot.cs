using System.Collections.Generic;

namespace TokenHound.Core.Models;

/// <summary>
/// Represents an immutable point-in-time snapshot of provider status, limit windows, and active blocks.
/// </summary>
public sealed record Snapshot
{
    /// <summary>
    /// Gets the unique identifier of the provider.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Gets the current operational status of the provider.
    /// </summary>
    public required ProviderStatus Status { get; init; }

    /// <summary>
    /// Gets the fidelity level of the snapshot metrics.
    /// </summary>
    public required Fidelity Fidelity { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the snapshot was captured.
    /// </summary>
    public required DateTimeOffset FetchedAtUtc { get; init; }

    /// <summary>
    /// Gets the collection of limit windows and quota buckets associated with the provider.
    /// </summary>
    public required IReadOnlyList<LimitWindow> LimitWindows { get; init; }

    /// <summary>
    /// Gets the active blocking condition, or <see langword="null"/> if the provider is unblocked.
    /// </summary>
    public UsageBlock? ActiveBlock { get; init; }

    /// <summary>
    /// Gets the error description or diagnostic message when the provider is in an error state.
    /// </summary>
    public string? ErrorDescription { get; init; }
}
