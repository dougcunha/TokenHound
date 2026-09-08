using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Engine;

public sealed partial class UsageStore
{
    private async Task RefreshNowCoreAsync(CancellationToken cancellationToken)
    {

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {

            await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {

            _refreshLock.Release();
        }
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {

        _lastAttemptUtc = _timeProvider.GetUtcNow();

        foreach (var provider in _providers.Values)
        {

            cancellationToken.ThrowIfCancellationRequested();

            await RefreshProviderAsync(provider, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshProviderAsync(IUsageProvider provider, CancellationToken cancellationToken)
    {

        var isRequestGated = provider is IRequestGatedUsageProvider;

        if (!isRequestGated && IsRateLimited(provider.ProviderId))
            return;

        try
        {

            var snapshot = await provider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

            if (!isRequestGated)
                await PersistRateLimitDeadlineAsync(snapshot, cancellationToken).ConfigureAwait(false);

            await StoreSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {

            await HandleRefreshFaultAsync(provider.ProviderId, ex, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task HandleRefreshFaultAsync(
        string providerId,
        Exception ex,
        CancellationToken cancellationToken)
    {

        if (ex is OperationCanceledException canceledEx)
            ProviderFaultLog.Aborted(providerId, canceledEx);
        else
            ProviderFaultLog.Failed(providerId, ex);

        await StoreSnapshotAsync(
            CreateErrorSnapshot(providerId, ex),
            cancellationToken
        ).ConfigureAwait(false);
    }


    private bool IsRateLimited(string providerId)
    {

        var nowUtc = _timeProvider.GetUtcNow();

        if (_backoffDeadlines.TryGetValue(providerId, out var persistedDeadline)
            && !RateLimitPolicy.CanDispatch(nowUtc, persistedDeadline))
        {
            RateLimitGate.IsBlocked(providerId, persistedDeadline);
            return true;
        }

        if (_snapshots.TryGetValue(providerId, out var currentSnapshot)
            && !RateLimitPolicy.CanDispatch(nowUtc, currentSnapshot.ActiveBlock?.ResetTimeUtc))
        {
            RateLimitGate.IsBlocked(providerId, currentSnapshot.ActiveBlock?.ResetTimeUtc);
            return true;
        }

        return false;
    }

    private async Task StoreSnapshotAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken)
    {

        var lastGoodSnapshot = _lastGoodSnapshots.TryGetValue(snapshot.ProviderId, out var archived)
            ? archived
            : null;
        _snapshots.TryGetValue(snapshot.ProviderId, out var previousSnapshot);
        RateLimitGate.LogTransition(previousSnapshot, snapshot);

        var decision = SnapshotRetentionPolicy.Apply(snapshot, lastGoodSnapshot);

        _snapshots[snapshot.ProviderId] = decision.CurrentSnapshot;

        if (decision.ClearsHistory)
        {
            _lastGoodSnapshots.TryRemove(snapshot.ProviderId, out _);
            if (_archive is not null)
                await _archive.ClearSnapshotAsync(snapshot.ProviderId, cancellationToken).ConfigureAwait(false);
        }
        else if (snapshot.Status == ProviderStatus.Ok && decision.ArchivedSnapshot is not null)
        {
            _lastGoodSnapshots[snapshot.ProviderId] = decision.ArchivedSnapshot;
            if (_archive is not null)
                await _archive.SaveSnapshotAsync(decision.ArchivedSnapshot, cancellationToken).ConfigureAwait(false);
        }

        SnapshotUpdated?.Invoke(this, decision.CurrentSnapshot);
    }

    private async Task PersistRateLimitDeadlineAsync(
        Snapshot snapshot,
        CancellationToken cancellationToken)
    {

        var block = snapshot.ActiveBlock;

        if (block?.IsBlocked != true || block.ResetTimeUtc is not { } deadlineUtc)
            return;

        if (!RateLimitPolicy.CanDispatch(_timeProvider.GetUtcNow(), deadlineUtc))
        {
            _backoffDeadlines[snapshot.ProviderId] = deadlineUtc;
            if (_archive is not null)
                await _archive.SaveBackoffDeadlineAsync(
                    snapshot.ProviderId,
                    deadlineUtc,
                    cancellationToken
                ).ConfigureAwait(false);
        }
    }

    private void LoadArchive()
    {

        var state = _archive!.Load();
        ArchiveErrorDescription = state.ErrorDescription;

        foreach (var entry in state.LastReadings)
        {

            _lastGoodSnapshots[entry.Key] = entry.Value;
            _snapshots[entry.Key] = CreateRestoredStaleSnapshot(entry.Value);
        }

        foreach (var entry in state.BackoffDeadlines)
            _backoffDeadlines[entry.Key] = entry.Value;
    }

    private static Snapshot CreateRestoredStaleSnapshot(Snapshot snapshot)
        => SnapshotRetentionPolicy.Apply(
            snapshot with
            {
                Status = ProviderStatus.Stale,
                ActiveBlock = null,
                ErrorDescription = null
            },
            snapshot
        ).CurrentSnapshot;

    private Snapshot CreateErrorSnapshot(string providerId, Exception ex)
        => _snapshots.TryGetValue(providerId, out var prev)
            ? new Snapshot
            {
                ProviderId = providerId,
                Status = ProviderStatus.Stale,
                Fidelity = prev.Fidelity,
                FetchedAtUtc = prev.FetchedAtUtc,
                LimitWindows = prev.LimitWindows,
                ActiveBlock = prev.ActiveBlock,
                CopilotBilling = CreateStaleBilling(prev.CopilotBilling),
                ErrorDescription = ex.Message
            }
            : new Snapshot
            {
                ProviderId = providerId,
                Status = ProviderStatus.Stale,
                Fidelity = Fidelity.Official,
                FetchedAtUtc = _timeProvider.GetUtcNow(),
                LimitWindows = [],
                ActiveBlock = null,
                CopilotBilling = null,
                ErrorDescription = ex.Message
            };

    private CopilotBillingStatus? CreateStaleBilling(CopilotBillingStatus? billing)
    {

        if (billing is null)
            return null;

        return billing with
        {
            State = CopilotBillingState.Stale,
            Reason = CopilotBillingReason.NetworkFailure,
            NextRequestAtUtc = _timeProvider.GetUtcNow()
        };
    }

    private void ThrowIfDisposedOrStopping()
    {

        if (_disposed || _lifetime.IsStoppingOrDisposed)
            throw new ObjectDisposedException(nameof(UsageStore), "The usage store is stopping or has been disposed.");
    }
}
