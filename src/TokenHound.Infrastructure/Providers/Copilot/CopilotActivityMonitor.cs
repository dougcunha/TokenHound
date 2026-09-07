using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.Storage;

namespace TokenHound.Infrastructure.Providers.Copilot;

/// <summary>
/// Monitors read-only Copilot session and log timestamps together with host liveness.
/// </summary>
public sealed class CopilotActivityMonitor : IActivityMonitor, IDisposable
{
    /// <summary>
    /// The unique provider identifier for Copilot.
    /// </summary>
    public const string PROVIDER_ID = "copilot";

    /// <summary>
    /// The maximum age of a qualifying activity write.
    /// </summary>
    public static readonly TimeSpan DEFAULT_BUSY_THRESHOLD = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The quiet period used to consolidate watcher notifications.
    /// </summary>
    public static readonly TimeSpan WATCHER_DEBOUNCE = TimeSpan.FromMilliseconds(120);

    private readonly string _sessionStateDirectory;
    private readonly string _logsDirectory;
    private readonly CopilotProcessHostDetector _hostDetector;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _busyThreshold;
    private readonly object _watcherLock = new();

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private int _debouncedNotificationCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a monitor using the current user's Copilot directories.
    /// </summary>
    /// <param name="baseDirectory">The user profile directory, or null for the current user.</param>
    /// <param name="hostDetector">An optional process host detector.</param>
    /// <param name="timeProvider">An optional clock for deterministic tests.</param>
    /// <param name="busyThreshold">The maximum age of a qualifying write.</param>
    /// <param name="enableWatcher">Whether to observe session-state write notifications.</param>
    public CopilotActivityMonitor(
        string? baseDirectory = null,
        CopilotProcessHostDetector? hostDetector = null,
        TimeProvider? timeProvider = null,
        TimeSpan? busyThreshold = null,
        bool enableWatcher = true)
    {

        var profileDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            : baseDirectory;
        _sessionStateDirectory = Path.Combine(profileDirectory, ".copilot", "session-state");
        _logsDirectory = Path.Combine(profileDirectory, ".copilot", "logs");
        _hostDetector = hostDetector ?? new CopilotProcessHostDetector(profileDirectory);
        _timeProvider = timeProvider ?? TimeProvider.System;
        _busyThreshold = busyThreshold ?? DEFAULT_BUSY_THRESHOLD;

        if (_busyThreshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(busyThreshold));

        if (enableWatcher)
            CreateWatcher();
    }

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Gets the session-state directory observed by this monitor.
    /// </summary>
    public string SessionStateDirectory
        => _sessionStateDirectory;

    /// <summary>
    /// Gets the logs directory observed by this monitor.
    /// </summary>
    public string LogsDirectory
        => _logsDirectory;

    /// <summary>
    /// Gets the number of consolidated watcher notifications.
    /// </summary>
    public int DebouncedNotificationCount
        => Volatile.Read(ref _debouncedNotificationCount);

    /// <summary>
    /// Occurs after a burst of session-state watcher notifications is consolidated.
    /// </summary>
    public event EventHandler? ActivityChanged;

    /// <inheritdoc />
    public ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var lastActivityUtc = FindLastActivityUtc();

        if (lastActivityUtc is null)
            return ValueTask.FromResult<AgentSession?>(null);

        var nowUtc = _timeProvider.GetUtcNow();

        if (!IsRecent(lastActivityUtc.Value, nowUtc)
            || !_hostDetector.TryGetQualifyingHost(out var host)
            || host is null)
            return ValueTask.FromResult<AgentSession?>(null);

        return ValueTask.FromResult<AgentSession?>(new AgentSession
        {
            Pid = host.Pid,
            StartTimeUtc = host.StartTimeUtc ?? nowUtc,
            State = AgentSessionState.Busy,
            LastActivityUtc = lastActivityUtc.Value
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {

        FileSystemWatcher? watcher;
        Timer? debounceTimer;

        lock (_watcherLock)
        {

            if (_disposed)
                return;

            _disposed = true;
            watcher = _watcher;
            debounceTimer = _debounceTimer;
            _watcher = null;
            _debounceTimer = null;
        }

        if (watcher is not null)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Changed -= OnWatcherEvent;
            watcher.Created -= OnWatcherEvent;
            watcher.Deleted -= OnWatcherEvent;
            watcher.Renamed -= OnWatcherRenamed;
            watcher.Dispose();
        }

        debounceTimer?.Dispose();
    }

    private bool IsRecent(DateTimeOffset lastActivityUtc, DateTimeOffset nowUtc)
    {

        var age = nowUtc - lastActivityUtc;

        return age >= TimeSpan.Zero && age <= _busyThreshold;
    }

    private DateTimeOffset? FindLastActivityUtc()
    {

        DateTimeOffset? latest = null;

        foreach (var path in GetActivityFiles())
        {

            var lastWriteUtc = GetLastWriteTimeUtc(path);

            if (lastWriteUtc is not null && (latest is null || lastWriteUtc > latest))
                latest = lastWriteUtc;
        }

        return latest;
    }

    private IReadOnlyList<string> GetActivityFiles()
    {

        var files = new List<string>();
        AddFiles(files, _sessionStateDirectory, "events.jsonl");
        AddFiles(files, _logsDirectory, "*");

        return files;
    }

    private static void AddFiles(
        ICollection<string> files,
        string directory,
        string pattern)
    {

        if (!Directory.Exists(directory))
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
                files.Add(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            // A disappearing or inaccessible activity directory means idle for this poll.
        }
    }

    private static DateTimeOffset? GetLastWriteTimeUtc(string path)
    {

        try
        {
            using var stream = SharedFileReader.OpenRead(path);
            var lastWriteUtc = File.GetLastWriteTimeUtc(path);

            return lastWriteUtc == DateTime.MinValue
                ? null
                : new DateTimeOffset(lastWriteUtc, TimeSpan.Zero);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            return null;
        }
    }

    private void CreateWatcher()
    {

        if (!Directory.Exists(_sessionStateDirectory))
            return;

        try
        {
            var watcher = new FileSystemWatcher(_sessionStateDirectory, "events.jsonl")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnWatcherEvent;
            watcher.Created += OnWatcherEvent;
            watcher.Deleted += OnWatcherEvent;
            watcher.Renamed += OnWatcherRenamed;
            _watcher = watcher;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {

            _watcher = null;
        }
    }

    private void OnWatcherEvent(object? sender, FileSystemEventArgs args)
        => ScheduleDebouncedNotification();

    private void OnWatcherRenamed(object? sender, RenamedEventArgs args)
        => ScheduleDebouncedNotification();

    private void ScheduleDebouncedNotification()
    {

        lock (_watcherLock)
        {

            if (_disposed)
                return;

            _debounceTimer ??= new Timer(
                static state => ((CopilotActivityMonitor)state!).PublishDebouncedNotification(),
                this,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan
            );
            _debounceTimer.Change(WATCHER_DEBOUNCE, Timeout.InfiniteTimeSpan);
        }
    }

    private void PublishDebouncedNotification()
    {

        if (_disposed)
            return;

        Interlocked.Increment(ref _debouncedNotificationCount);
        ActivityChanged?.Invoke(this, EventArgs.Empty);
    }
}
