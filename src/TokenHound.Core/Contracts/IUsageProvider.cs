using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Models;

namespace TokenHound.Core.Contracts;

/// <summary>
/// Defines a provider adapter that retrieves real-time quota, usage, and rate-limit snapshots.
/// </summary>
public interface IUsageProvider
{
    /// <summary>
    /// Gets the unique identifier of the provider.
    /// </summary>
    string ProviderId { get; }

    /// <summary>
    /// Retrieves a point-in-time snapshot of the provider's usage and quota metrics.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A <see cref="ValueTask{Snapshot}"/> containing the usage snapshot.</returns>
    ValueTask<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
