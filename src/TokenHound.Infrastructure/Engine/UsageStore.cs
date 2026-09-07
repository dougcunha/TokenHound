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
public sealed partial class UsageStore : IDisposable
{
    private readonly ConcurrentDictionary<string, IUsageProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Snapshot> _snapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Snapshot> _lastGoodSnapshots = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _backoffDeadlines = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<IActivityMonitor> _monitors = [];
    private readonly object _monitorsLock = new();
    private readonly object _activityLock = new();
    private readonly Dictionary<string, AgentSession?> _activityStates = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly UsageStoreLifetime _lifetime = new();
    private readonly UsageArchive? _archive;
    private readonly bool _ownsArchive;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _idleInterval;
    private readonly TimeSpan _activityPollInterval;

    private DateTimeOffset? _lastAttemptUtc;
    private bool _disposed;

    /// <summary>
    /// Default activity polling interval (2 seconds).
    /// </summary>
    public static readonly TimeSpan DEFAULT_ACTIVITY_INTERVAL = TimeSpan.FromSeconds(2);

    /// <summary>Initializes a new instance of the <see cref="UsageStore"/> class.</summary>
    /// <param name="autoStart">Whether to start the periodic polling timer immediately.</param>
    /// <param name="idleInterval">The idle polling interval threshold, or null for default 300s.</param>
    /// <param name="pollInterval">The timer tick interval, or null for the default active interval.</param>
    /// <param name="archive">An optional TokenHound-owned archive.</param>
    /// <param name="timeProvider">An optional clock for deterministic scheduling.</param>
    /// <param name="activityPollInterval">The activity polling interval, or null for 2s.</param>
    public UsageStore(
        bool autoStart = false,
        TimeSpan? idleInterval = null,
        TimeSpan? pollInterval = null,
        UsageArchive? archive = null,
        TimeProvider? timeProvider = null,
        TimeSpan? activityPollInterval = null)
    {

        _idleInterval = idleInterval ?? RefreshSchedulePolicy.DEFAULT_IDLE_INTERVAL;
        _archive = archive;
        _ownsArchive = archive is not null;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _activityPollInterval = activityPollInterval ?? DEFAULT_ACTIVITY_INTERVAL;

        if (_archive is not null)
            LoadArchive();

        if (autoStart)
            Start(pollInterval, _activityPollInterval);
    }

    /// <summary>Occurs whenever a provider snapshot is fetched or updated.</summary>
    public event EventHandler<Snapshot>? SnapshotUpdated;

    /// <summary>Occurs whenever a registered activity monitor changes state.</summary>
    public event EventHandler<ProviderActivityChangedEventArgs>? ActivityUpdated;

    /// <summary>Gets the cache of current snapshots indexed by provider identifier.</summary>
    public IReadOnlyDictionary<string, Snapshot> CurrentSnapshots
        => _snapshots;

    /// <summary>Gets a value indicating whether the polling timer is currently running.</summary>
    public bool IsRunning
        => _lifetime.IsTimerRunning;

    /// <summary>
    /// Gets a value indicating whether the activity polling timer is currently running.
    /// </summary>
    public bool IsActivityRunning
        => _lifetime.IsActivityTimerRunning;

    /// <summary>
    /// Gets a diagnostic from archive loading, or null when archive files were readable.
    /// </summary>
    public string? ArchiveErrorDescription { get; private set; }

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

        var isAnyBusy = HasActivityState
            ? HasBusyActivity
            : await CheckAnyBusyAsync(lease.Token).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var elapsed = _lastAttemptUtc.HasValue ? now - _lastAttemptUtc.Value : TimeSpan.MaxValue;

        if (!RefreshSchedulePolicy.ShouldRefresh(isAnyBusy, elapsed, _idleInterval))
            return;

        await RefreshNowCoreAsync(lease.Token).ConfigureAwait(false);
    }

    /// <summary>Starts the background periodic polling timer.</summary>
    /// <param name="pollInterval">The polling interval, or null for the default active interval.</param>
    /// <param name="activityPollInterval">The activity polling interval, or null for default 2 seconds.</param>
    public void Start(
        TimeSpan? pollInterval = null,
        TimeSpan? activityPollInterval = null)
    {

        ThrowIfDisposedOrStopping();

        var interval = pollInterval ?? RefreshSchedulePolicy.DEFAULT_ACTIVE_INTERVAL;
        _lifetime.StartTimer(interval, TickAsync);
        _lifetime.StartActivityTimer(
            activityPollInterval ?? _activityPollInterval,
            PollActivityAsync);
    }

    /// <summary>Stops the background periodic polling timer.</summary>
    public void Stop()
    {

        _lifetime.StopTimer();
        _lifetime.StopActivityTimer();
    }

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
    {

        _refreshLock.Dispose();

        if (_ownsArchive)
            _archive?.Dispose();
    }
}
