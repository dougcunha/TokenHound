using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.System;

namespace TokenHound.Infrastructure.Providers.Cline;

/// <summary>
/// Monitors Cline hub or CLI liveness and the recent activity recorded in its local session database.
/// </summary>
/// <remarks>
/// The hub daemon advertises its own process identifier in its lock file, which is a stronger liveness
/// signal than a name lookup, so it is resolved first and the process name is only a fallback.
/// </remarks>
public sealed class ClineActivityMonitor : IActivityMonitor
{
    /// <summary>The unique provider identifier for Cline.</summary>
    public const string PROVIDER_ID = "cline";

    /// <summary>The process names monitored as a Cline liveness fallback.</summary>
    public static readonly string[] MONITORED_PROCESS_NAMES = ["cline", "cline-cli", "cline.exe"];

    private static readonly TimeSpan BUSY_THRESHOLD = TimeSpan.FromSeconds(60);

    private readonly Func<CancellationToken, Task<DateTimeOffset?>> _activityReader;
    private readonly Func<CancellationToken, Task<ClineHubSnapshot?>> _hubReader;
    private readonly Func<(int Pid, DateTimeOffset StartTimeUtc)?> _processLocator;
    private readonly Func<int, DateTimeOffset, bool> _processLiveness;
    private readonly TimeProvider _timeProvider;

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineActivityMonitor"/> class.
    /// </summary>
    /// <param name="processLocator">Optional delegate to locate a running Cline process.</param>
    /// <param name="timeProvider">Optional clock used to evaluate activity age.</param>
    /// <param name="activityReader">Optional reader for the latest local session activity.</param>
    /// <param name="processLiveness">Optional PID and start-time validator.</param>
    /// <param name="hubReader">Optional reader for the Cline hub lock file.</param>
    public ClineActivityMonitor(
        Func<(int Pid, DateTimeOffset StartTimeUtc)?>? processLocator = null,
        TimeProvider? timeProvider = null,
        Func<CancellationToken, Task<DateTimeOffset?>>? activityReader = null,
        Func<int, DateTimeOffset, bool>? processLiveness = null,
        Func<CancellationToken, Task<ClineHubSnapshot?>>? hubReader = null)
    {

        var reader = new ClineActivityReader();

        _processLocator = processLocator ?? DefaultProcessLocator;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _processLiveness = processLiveness ?? (static (pid, startTimeUtc) =>
            ProcessLiveness.IsProcessAlive(pid, startTimeUtc));
        _activityReader = activityReader ?? reader.ReadLastActivityUtcAsync;
        _hubReader = hubReader ?? reader.ReadHubAsync;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClineActivityMonitor"/> class with a process name finder.
    /// </summary>
    /// <param name="processByNameFinder">Delegate to locate a running process by its name.</param>
    /// <param name="timeProvider">Optional clock used to evaluate activity age.</param>
    /// <param name="activityReader">Optional reader for the latest local session activity.</param>
    /// <param name="processLiveness">Optional PID and start-time validator.</param>
    /// <param name="hubReader">Optional reader for the Cline hub lock file.</param>
    public ClineActivityMonitor(
        Func<string, (int Pid, DateTimeOffset StartTimeUtc)?> processByNameFinder,
        TimeProvider? timeProvider = null,
        Func<CancellationToken, Task<DateTimeOffset?>>? activityReader = null,
        Func<int, DateTimeOffset, bool>? processLiveness = null,
        Func<CancellationToken, Task<ClineHubSnapshot?>>? hubReader = null)
        : this(
            () => FindFromFinder(processByNameFinder),
            timeProvider,
            activityReader,
            processLiveness,
            hubReader)
    {
    }

    /// <inheritdoc />
    public async ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();

        var process = await ResolveProcessAsync(cancellationToken).ConfigureAwait(false);

        if (process is null || process.Value.Pid <= 0 ||
            !_processLiveness(process.Value.Pid, process.Value.StartTimeUtc))
            return null;

        var nowUtc = _timeProvider.GetUtcNow();
        var lastActivityUtc = await _activityReader(cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        return new AgentSession
        {
            Pid = process.Value.Pid,
            StartTimeUtc = process.Value.StartTimeUtc,
            State = IsRecent(lastActivityUtc, nowUtc)
                ? AgentSessionState.Busy
                : AgentSessionState.Idle,
            LastActivityUtc = lastActivityUtc ?? process.Value.StartTimeUtc
        };
    }

    private async Task<(int Pid, DateTimeOffset StartTimeUtc)?> ResolveProcessAsync(
        CancellationToken cancellationToken)
    {

        var hub = await _hubReader(cancellationToken).ConfigureAwait(false);

        if (hub is { Pid: > 0 } && _processLiveness(hub.Pid, hub.StartedAtUtc))
            return (hub.Pid, hub.StartedAtUtc);

        return _processLocator();
    }

    private static bool IsRecent(DateTimeOffset? lastActivityUtc, DateTimeOffset nowUtc)
    {

        if (lastActivityUtc is null)
            return false;

        var age = nowUtc - lastActivityUtc.Value;

        return age >= TimeSpan.Zero && age <= BUSY_THRESHOLD;
    }

    private static (int Pid, DateTimeOffset StartTimeUtc)? DefaultProcessLocator()
        => FindFromFinder(ProcessDiscovery.FindProcessByName);

    private static (int Pid, DateTimeOffset StartTimeUtc)? FindFromFinder(
        Func<string, (int Pid, DateTimeOffset StartTimeUtc)?> finder)
    {

        foreach (var name in MONITORED_PROCESS_NAMES)
        {

            var process = finder(name);

            if (process is not null && process.Value.Pid > 0)
                return process;
        }

        return null;
    }
}