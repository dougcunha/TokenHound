using System;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageStore
{
    internal MetricsState? GetMetricsState(string providerId)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);

        lock (_snapshotStateLock)
        {

            if (!_snapshots.TryGetValue(providerId, out var snapshot))
                return null;

            var lastSuccessfulAtUtc = _lastGoodSnapshots.TryGetValue(providerId, out var lastGoodSnapshot)
                ? lastGoodSnapshot.FetchedAtUtc
                : (DateTimeOffset?)null;

            return new MetricsState(snapshot, lastSuccessfulAtUtc);
        }
    }

    internal readonly record struct MetricsState(Snapshot Snapshot, DateTimeOffset? LastSuccessfulAtUtc);
}
