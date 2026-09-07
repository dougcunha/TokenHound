using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Core.Policies;

namespace TokenHound.Infrastructure.Engine;

/// <summary>Central coordinator managing polling schedule, provider polling, rate limits, and snapshot events.</summary>
public sealed class UsageStore : IDisposable
{
    private readonly ConcurrentDictionary<string, IUsageProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Snapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IActivityMonitor> _monitors = [];
    private readonly object _monitorsLock = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly UsageStoreLifetime _lifetime = new();
    private readonly TimeSpan _idleInterval;

    private DateTimeOffset? _lastAttemptUtc;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="UsageStore"/> class.</summary>
    /// <param name="autoStart">Whether to start the periodic polling timer immediately.</param>
    /// <param name="idleInterval">The idle polling interval threshold, or null for default 300s.</param>
    /// <param name="pollInterval">The timer tick interval, or null for default 60s.</param>
    public UsageStore(
        bool autoStart = false,
        TimeSpan? idleInterval = null,
        TimeSpan? pollInterval = null)
    {

        _idleInterval = idleInterval ?? RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL;

        if (autoStart)
            Start(pollInterval);
    }

    /// <summary>Occurs whenever a provider snapshot is fetched or updated.</summary>
    public event EventHandler<Snapshot>? SnapshotUpdated;

    /// <summary>Gets the cache of current snapshots indexed by provider identifier.</summary>
    public IReadOnlyDictionary<string, Snapshot> CurrentSnapshots
        => _snapshots;

    /// <summary>Gets a value indicating whether the polling timer is currently running.</summary>
    public bool IsRunning
        => _lifetime.IsTimerRunning;

    /// <summary>Registers a provider adapter to be polled for usage snapshots.</summary>
    /// <param name="provider">The provider adapter to register.</param>
    public void RegisterProvider(IUsageProvider provider)
    {

        ThrowIfDisposedOrStopping();
        ArgumentNullException.ThrowIfNull(provider);

        _providers[provider.ProviderId] = provider;
    }

    /// <summary>Registers an activity monitor used to evaluate session busyness.</summary>
    /// <param name="monitor">The activity monitor to register.</param>
    public void RegisterActivityMonitor(IActivityMonitor monitor)
    {

        ThrowIfDisposedOrStopping();
        ArgumentNullException.ThrowIfNull(monitor);

        lock (_monitorsLock)
        {

            if (!_monitors.Contains(monitor))
                _monitors.Add(monitor);
        }
    }

    /// <summary>Forces an immediate refresh of all registered providers, bypassing schedule checks.</summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RefreshNowAsync(CancellationToken cancellationToken = default)
    {

        ThrowIfDisposedOrStopping();
        cancellationToken.ThrowIfCancellationRequested();

        using var lease = _lifetime.Enter(cancellationToken);

        await RefreshNowCoreAsync(lease.Token).ConfigureAwait(false);
    }

    /// <summary>Evaluates the schedule policy and refreshes providers if eligible.</summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {

        ThrowIfDisposedOrStopping();
        cancellationToken.ThrowIfCancellationRequested();

        using var lease = _lifetime.Enter(cancellationToken);

        var isAnyBusy = await CheckAnyBusyAsync(lease.Token).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var elapsed = _lastAttemptUtc.HasValue ? now - _lastAttemptUtc.Value : TimeSpan.MaxValue;

        if (!RefreshSchedulePolicy.ShouldRefresh(isAnyBusy, elapsed, _idleInterval))
            return;

        await RefreshNowCoreAsync(lease.Token).ConfigureAwait(false);
    }

    /// <summary>Starts the background periodic polling timer.</summary>
    /// <param name="pollInterval">The polling interval, or null for default 60 seconds.</param>
    public void Start(TimeSpan? pollInterval = null)
    {

        ThrowIfDisposedOrStopping();

        var interval = pollInterval ?? RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL;
        _lifetime.StartTimer(interval, TickAsync);
    }

    /// <summary>Stops the background periodic polling timer.</summary>
    public void Stop()
        => _lifetime.StopTimer();

    /// <summary>Terminates the store asynchronously, closing admission, draining operations, and releasing resources.</summary>
    /// <param name="cancellationToken">A token to observe while awaiting the terminal drain.</param>
    /// <returns>A task representing the asynchronous terminal stop operation.</returns>
    public Task StopAsync(CancellationToken cancellationToken = default)
        => _lifetime.StopAsync(ReleaseResources, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        _ = StopAsync(CancellationToken.None);
    }

    private void ReleaseResources()
        => _refreshLock.Dispose();

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

        _lastAttemptUtc = DateTimeOffset.UtcNow;

        foreach (var provider in _providers.Values)
        {

            cancellationToken.ThrowIfCancellationRequested();

            await RefreshProviderAsync(provider, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RefreshProviderAsync(IUsageProvider provider, CancellationToken cancellationToken)
    {

        if (IsRateLimited(provider.ProviderId))
            return;

        try
        {

            var snapshot = await provider.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);

            StoreSnapshot(snapshot);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {

            StoreSnapshot(CreateErrorSnapshot(provider.ProviderId, ex));
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch (Exception ex)
        {

            StoreSnapshot(CreateErrorSnapshot(provider.ProviderId, ex));
        }
    }

    private bool IsRateLimited(string providerId)
        => _snapshots.TryGetValue(providerId, out var currentSnapshot)
            && !RateLimitPolicy.CanDispatch(DateTimeOffset.UtcNow, currentSnapshot.ActiveBlock?.ResetTimeUtc);

    private void StoreSnapshot(Snapshot snapshot)
    {

        _snapshots[snapshot.ProviderId] = snapshot;
        SnapshotUpdated?.Invoke(this, snapshot);
    }

    private async ValueTask<bool> CheckAnyBusyAsync(CancellationToken cancellationToken)
    {

        IActivityMonitor[] monitors;

        lock (_monitorsLock)
            monitors = [.. _monitors];

        foreach (var monitor in monitors)
        {

            cancellationToken.ThrowIfCancellationRequested();

            if (await IsBusyAsync(monitor, cancellationToken).ConfigureAwait(false))
                return true;
        }

        return false;
    }

    private static async ValueTask<bool> IsBusyAsync(IActivityMonitor monitor, CancellationToken cancellationToken)
    {

        try
        {

            var session = await monitor.CheckLivenessAsync(cancellationToken).ConfigureAwait(false);

            return session?.State == AgentSessionState.Busy;
        }
        catch (OperationCanceledException)
        {

            throw;
        }
        catch
        {

            return false;
        }
    }

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
                ErrorDescription = ex.Message
            }
            : new Snapshot
            {
                ProviderId = providerId,
                Status = ProviderStatus.Stale,
                Fidelity = Fidelity.Official,
                FetchedAtUtc = DateTimeOffset.UtcNow,
                LimitWindows = [],
                ActiveBlock = null,
                ErrorDescription = ex.Message
            };

    private void ThrowIfDisposedOrStopping()
    {

        if (_disposed || _lifetime.IsStoppingOrDisposed)
            throw new ObjectDisposedException(nameof(UsageStore), "The usage store is stopping or has been disposed.");
    }
}
