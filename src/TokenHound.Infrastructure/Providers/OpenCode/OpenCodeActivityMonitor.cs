using System;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;
using TokenHound.Infrastructure.System;

namespace TokenHound.Infrastructure.Providers.OpenCode;

/// <summary>
/// Monitors OpenCode process liveness and recent activity recorded in its local database.
/// </summary>
public sealed class OpenCodeActivityMonitor : IActivityMonitor
{
    private static readonly TimeSpan BUSY_THRESHOLD = TimeSpan.FromSeconds(60);

    /// <summary>The unique provider identifier for OpenCode.</summary>
    public const string PROVIDER_ID = "opencode";

    /// <summary>The process names monitored for OpenCode activity.</summary>
    public static readonly string[] MONITORED_PROCESS_NAMES = ["opencode", "OpenCode"];

    private readonly Func<(int Pid, DateTimeOffset StartTimeUtc)?> _processLocator;
    private readonly Func<CancellationToken, Task<DateTimeOffset?>> _activityReader;
    private readonly Func<int, DateTimeOffset, bool> _processLiveness;
    private readonly TimeProvider _timeProvider;

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeActivityMonitor"/> class.
    /// </summary>
    /// <param name="processLocator">Optional delegate to locate a running OpenCode process.</param>
    /// <param name="timeProvider">Optional clock used to evaluate activity age.</param>
    /// <param name="activityReader">Optional reader for the latest OpenCode database activity.</param>
    /// <param name="processLiveness">Optional PID and start-time validator.</param>
    public OpenCodeActivityMonitor(
        Func<(int Pid, DateTimeOffset StartTimeUtc)?>? processLocator = null,
        TimeProvider? timeProvider = null,
        Func<CancellationToken, Task<DateTimeOffset?>>? activityReader = null,
        Func<int, DateTimeOffset, bool>? processLiveness = null)
    {

        _processLocator = processLocator ?? DefaultProcessLocator;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _processLiveness = processLiveness ?? (static (pid, startTimeUtc) =>
            ProcessLiveness.IsProcessAlive(pid, startTimeUtc));
        var databaseReader = new OpenCodeActivityReader();
        _activityReader = activityReader ?? databaseReader.ReadLastActivityUtcAsync;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenCodeActivityMonitor"/> class with a process name finder.
    /// </summary>
    /// <param name="processByNameFinder">Delegate to locate a running process by its name.</param>
    /// <param name="timeProvider">Optional clock used to evaluate activity age.</param>
    /// <param name="activityReader">Optional reader for the latest OpenCode database activity.</param>
    /// <param name="processLiveness">Optional PID and start-time validator.</param>
    public OpenCodeActivityMonitor(
        Func<string, (int Pid, DateTimeOffset StartTimeUtc)?> processByNameFinder,
        TimeProvider? timeProvider = null,
        Func<CancellationToken, Task<DateTimeOffset?>>? activityReader = null,
        Func<int, DateTimeOffset, bool>? processLiveness = null)
        : this(
            () => FindFromFinder(processByNameFinder),
            timeProvider,
            activityReader,
            processLiveness)
    {
    }

    /// <inheritdoc />
    public async ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {

        cancellationToken.ThrowIfCancellationRequested();
        var process = _processLocator();

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
