using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.App.ViewModels;
using TokenHound.Infrastructure.HudFeed;

namespace TokenHound.App.HudFeed;

/// <summary>
/// Publishes the HUD rings to the HUD feed: one coalesced write after HUD changes, a heartbeat write while
/// TokenHound runs, and a final write with <c>isRunning = false</c> on shutdown.
/// </summary>
public sealed class HudFeedPublisher : IAsyncDisposable
{
    /// <summary>The default delay that merges a burst of HUD changes into one write.</summary>
    public static readonly TimeSpan DEFAULT_COALESCE_DELAY = TimeSpan.FromMilliseconds(500);

    /// <summary>The default longest interval between two writes.</summary>
    public static readonly TimeSpan DEFAULT_HEARTBEAT = TimeSpan.FromSeconds(30);

    private readonly ObservableCollection<ProviderRingViewModel> _rings;
    private readonly Func<HudFeedDocument, CancellationToken, Task> _sink;
    private readonly Action<Action> _uiDispatcher;
    private readonly string _appVersion;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _coalesceDelay;
    private readonly TimeSpan _heartbeat;
    private readonly HashSet<ProviderRingViewModel> _attachedRings = [];
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private ITimer? _coalesceTimer;
    private ITimer? _heartbeatTimer;
    private HudFeedDocument? _lastDocument;
    private int _pending;
    private bool _hasLoggedFailure;
    private volatile bool _disposed;

    /// <summary>Creates a stopped publisher over the HUD rings.</summary>
    /// <param name="rings">The HUD rings, changed only on the UI thread.</param>
    /// <param name="sink">The write target, such as <see cref="HudFeedWriter.WriteAsync"/>.</param>
    /// <param name="uiDispatcher">Runs an action on the UI thread and returns after it completes.</param>
    /// <param name="appVersion">The TokenHound version written in the document.</param>
    /// <param name="timeProvider">The clock and timer source, or null for the system clock.</param>
    /// <param name="coalesceDelay">The burst merge delay, or null for the default.</param>
    /// <param name="heartbeat">The heartbeat interval, or null for the default.</param>
    public HudFeedPublisher(
        ObservableCollection<ProviderRingViewModel> rings,
        Func<HudFeedDocument, CancellationToken, Task> sink,
        Action<Action> uiDispatcher,
        string appVersion,
        TimeProvider? timeProvider = null,
        TimeSpan? coalesceDelay = null,
        TimeSpan? heartbeat = null)
    {

        _rings = rings ?? throw new ArgumentNullException(nameof(rings));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _appVersion = appVersion ?? throw new ArgumentNullException(nameof(appVersion));
        _timeProvider = timeProvider ?? TimeProvider.System;
        _coalesceDelay = coalesceDelay ?? DEFAULT_COALESCE_DELAY;
        _heartbeat = heartbeat ?? DEFAULT_HEARTBEAT;
    }

    /// <summary>Subscribes to the HUD rings and starts the timers. Call on the UI thread.</summary>
    public void Start()
    {

        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_coalesceTimer is not null)
            return;

        _rings.CollectionChanged += OnRingsChanged;
        SyncRingSubscriptions();
        _coalesceTimer = _timeProvider.CreateTimer(OnTimer, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _heartbeatTimer = _timeProvider.CreateTimer(OnTimer, null, _heartbeat, _heartbeat);
        RequestPublish();
    }

    /// <summary>Builds the document on the UI thread and writes it now.</summary>
    /// <returns>A task that completes after the write, or at once after dispose.</returns>
    internal async Task PublishNowAsync()
    {

        Interlocked.Exchange(ref _pending, 0);

        if (_disposed)
            return;

        var document = BuildDocument();

        if (document is null)
            return;

        await WriteAsync(document).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {

        if (_disposed)
            return;

        _disposed = true;
        _coalesceTimer?.Dispose();
        _heartbeatTimer?.Dispose();
        _rings.CollectionChanged -= OnRingsChanged;

        foreach (var ring in _attachedRings.ToArray())
            ring.PropertyChanged -= OnRingChanged;

        _attachedRings.Clear();

        await _writeLock.WaitAsync().ConfigureAwait(false);
        var last = _lastDocument;
        _writeLock.Release();

        if (last is not null)
            await WriteAsync(last with { IsRunning = false, WrittenAtUtc = _timeProvider.GetUtcNow() }).ConfigureAwait(false);
    }

    private void OnRingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {

        SyncRingSubscriptions();
        RequestPublish();
    }

    private void OnRingChanged(object? sender, PropertyChangedEventArgs e)
        => RequestPublish();

    private void SyncRingSubscriptions()
    {

        foreach (var ring in _attachedRings.Where(ring => !_rings.Contains(ring)).ToArray())
        {

            ring.PropertyChanged -= OnRingChanged;
            _attachedRings.Remove(ring);
        }

        foreach (var ring in _rings.Where(ring => _attachedRings.Add(ring)))
            ring.PropertyChanged += OnRingChanged;
    }

    private void RequestPublish()
    {

        if (_disposed || Interlocked.Exchange(ref _pending, 1) == 1)
            return;

        _coalesceTimer?.Change(_coalesceDelay, Timeout.InfiniteTimeSpan);
    }

    private void OnTimer(object? state)
        => _ = PublishSafelyAsync();

    private async Task PublishSafelyAsync()
    {

        try
        {

            await PublishNowAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {

            Log.Warning(ex, "HUD feed could not be built");
        }
    }

    private HudFeedDocument? BuildDocument()
    {

        HudFeedDocument? document = null;
        var now = _timeProvider.GetUtcNow();

        _uiDispatcher(() =>
        {

            if (!_disposed)
                document = HudFeedProjection.Create(_rings, now, _appVersion, _heartbeat);
        });

        return document;
    }

    private async Task WriteAsync(HudFeedDocument document)
    {

        await _writeLock.WaitAsync().ConfigureAwait(false);

        try
        {

            // A build that started before dispose must not replace the final isRunning = false document.
            if (_disposed && document.IsRunning)
                return;

            await _sink(document, CancellationToken.None).ConfigureAwait(false);
            _lastDocument = document;
            _hasLoggedFailure = false;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {

            if (!_hasLoggedFailure)
                Log.Warning(ex, "HUD feed write failed; the HUD continues without it");

            _hasLoggedFailure = true;
        }
        finally
        {

            _writeLock.Release();
        }
    }
}
