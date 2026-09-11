using System;
using TokenHound.Core.Models;

namespace TokenHound.Core.Policies;

/// <summary>
/// Applies pure status and history rules to provider snapshots.
/// </summary>
public static class SnapshotRetentionPolicy
{
    /// <summary>
    /// Applies the retention rule to an incoming snapshot and the last successful snapshot.
    /// </summary>
    /// <param name="incomingSnapshot">The snapshot produced by the latest provider attempt.</param>
    /// <param name="lastGoodSnapshot">The last successful snapshot, if one exists.</param>
    /// <returns>The effective current snapshot and the archive action.</returns>
    public static Decision Apply(
        Snapshot incomingSnapshot,
        Snapshot? lastGoodSnapshot)
    {

        ArgumentNullException.ThrowIfNull(incomingSnapshot);

        if (incomingSnapshot.Status is ProviderStatus.NeedsAuth or ProviderStatus.Unsupported or ProviderStatus.NotRunning)
            return CreateClearedDecision(incomingSnapshot);

        if (incomingSnapshot.Status == ProviderStatus.Ok)
            return CreateSuccessfulDecision(incomingSnapshot);

        var currentSnapshot = CreateStaleSnapshot(incomingSnapshot, lastGoodSnapshot);

        return CreateStaleDecision(currentSnapshot, lastGoodSnapshot);
    }

    private static Decision CreateClearedDecision(Snapshot incomingSnapshot)
        => new()
        {
            CurrentSnapshot = incomingSnapshot,
            ArchivedSnapshot = null,
            ClearsHistory = true
        };

    private static Decision CreateSuccessfulDecision(Snapshot incomingSnapshot)
        => new()
        {
            CurrentSnapshot = incomingSnapshot,
            ArchivedSnapshot = incomingSnapshot with { CopilotBilling = null },
            ClearsHistory = false
        };

    private static Decision CreateStaleDecision(
        Snapshot currentSnapshot,
        Snapshot? lastGoodSnapshot)
        => new()
        {
            CurrentSnapshot = currentSnapshot,
            ArchivedSnapshot = lastGoodSnapshot,
            ClearsHistory = false
        };

    private static Snapshot CreateStaleSnapshot(
        Snapshot incomingSnapshot,
        Snapshot? lastGoodSnapshot)
    {

        if (lastGoodSnapshot is null)
            return incomingSnapshot with
            {
                Status = ProviderStatus.Stale,
                LimitWindows = [],
                CopilotBilling = incomingSnapshot.CopilotBilling
            };

        return lastGoodSnapshot with
        {
            Status = ProviderStatus.Stale,
            ActiveBlock = incomingSnapshot.ActiveBlock,
            ErrorDescription = incomingSnapshot.ErrorDescription,
            CopilotBilling = incomingSnapshot.CopilotBilling
        };
    }

    /// <summary>
    /// Describes the current snapshot and archive result selected by the policy.
    /// </summary>
    public sealed record Decision
    {
        /// <summary>
        /// Gets the snapshot that should be exposed as the current provider state.
        /// </summary>
        public required Snapshot CurrentSnapshot { get; init; }

        /// <summary>
        /// Gets the snapshot that should remain in the last-good archive, or null when it must be cleared.
        /// </summary>
        public Snapshot? ArchivedSnapshot { get; init; }

        /// <summary>
        /// Gets a value indicating whether the existing archive must be removed.
        /// </summary>
        public required bool ClearsHistory { get; init; }
    }
}
