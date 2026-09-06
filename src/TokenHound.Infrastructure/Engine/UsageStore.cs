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
    private readonly TimeSpan _idleInterval;

    private CancellationTokenSource? _timerCts;
    private Task? _timerTask;
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
        => _timerTask is not null && !(_timerCts?.IsCancellationRequested ?? true);

    /// <summary>Registers a provider adapter to be polled for usage snapshots.</summary>
    /// <param name="provider">The provider adapter to register.</param>
    public void RegisterProvider(IUsageProvider provider)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(provider);

        _providers[provider.ProviderId] = provider;
    }

    /// <summary>Registers an activity monitor used to evaluate session busyness.</summary>
    /// <param name="monitor">The activity monitor to register.</param>
    public void RegisterActivityMonitor(IActivityMonitor monitor)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);
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

        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

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

    /// <summary>Evaluates the schedule policy and refreshes providers if eligible.</summary>
    /// <param name="cancellationToken">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();

        var isAnyBusy = await CheckAnyBusyAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;
        var elapsed = _lastAttemptUtc.HasValue ? now - _lastAttemptUtc.Value : TimeSpan.MaxValue;

        if (!RefreshSchedulePolicy.ShouldRefresh(isAnyBusy, elapsed, _idleInterval))
            return;

        await RefreshNowAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Starts the background periodic polling timer.</summary>
    /// <param name="pollInterval">The polling interval, or null for default 60 seconds.</param>
    public void Start(TimeSpan? pollInterval = null)
    {

        ObjectDisposedException.ThrowIf(_disposed, this);

        Stop();

        var interval = pollInterval ?? RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL;
        var cts = new CancellationTokenSource();
        _timerCts = cts;
        _timerTask = RunTimerLoopAsync(interval, cts.Token);
    }

    /// <summary>Stops the background periodic polling timer.</summary>
    public void Stop()
    {

        var cts = _timerCts;
        _timerCts = null;

        if (cts is not null)
        {

            cts.Cancel();
            cts.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {

        if (_disposed)
            return;

        _disposed = true;
        Stop();
        _refreshLock.Dispose();
    }

    private async Task RunTimerLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {

        using var timer = new PeriodicTimer(interval);

        try
        {

            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {

                await TickAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {

            // Normal cancellation on stop or disposal
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
    {

        if (!_snapshots.TryGetValue(providerId, out var currentSnapshot))
            return false;

        return !RateLimitPolicy.CanDispatch(DateTimeOffset.UtcNow, currentSnapshot.ActiveBlock?.ResetTimeUtc);
    }

    private void StoreSnapshot(Snapshot snapshot)
    {

        _snapshots[snapshot.ProviderId] = snapshot;
        SnapshotUpdated?.Invoke(this, snapshot);
    }

    private async ValueTask<bool> CheckAnyBusyAsync(CancellationToken cancellationToken)
    {

        IActivityMonitor[] monitors;

        lock (_monitorsLock)
        {

            monitors = [.. _monitors];
        }

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
        => new()
        {
            ProviderId = providerId,
            Status = ProviderStatus.Stale,
            Fidelity = Fidelity.Official,
            FetchedAtUtc = DateTimeOffset.UtcNow,
            LimitWindows = _snapshots.TryGetValue(providerId, out var prev) ? prev.LimitWindows : [],
            ActiveBlock = null,
            ErrorDescription = ex.Message
        };
}
