using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TokenHound.Core.Contracts;
using TokenHound.Core.Models;

namespace TokenHound.Infrastructure.Providers.Cursor;

/// <summary>
/// Monitors local Cursor editor activity by checking process liveness and Composer run states.
/// </summary>
public sealed class CursorActivityMonitor : IActivityMonitor
{
    private const string PROVIDER_ID = "cursor";
    private static readonly TimeSpan DEFAULT_STALE_THRESHOLD = TimeSpan.FromMinutes(15);

    private readonly CursorComposerReader _composerReader;
    private readonly Func<(int Pid, DateTimeOffset StartTimeUtc)?> _processLocator;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _staleThreshold;

    /// <inheritdoc />
    public string ProviderId
        => PROVIDER_ID;

    /// <summary>
    /// Initializes a new instance of the <see cref="CursorActivityMonitor"/> class.
    /// </summary>
    /// <param name="composerReader">Optional composer reader instance.</param>
    /// <param name="processLocator">Optional delegate to locate the running Cursor process.</param>
    /// <param name="timeProvider">Optional time provider for testing.</param>
    /// <param name="staleThreshold">Optional staleness threshold for unfinished runs.</param>
    public CursorActivityMonitor(
        CursorComposerReader? composerReader = null,
        Func<(int Pid, DateTimeOffset StartTimeUtc)?>? processLocator = null,
        TimeProvider? timeProvider = null,
        TimeSpan? staleThreshold = null)
    {
        _composerReader = composerReader ?? new CursorComposerReader();
        _processLocator = processLocator ?? DefaultProcessLocator;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _staleThreshold = staleThreshold ?? DEFAULT_STALE_THRESHOLD;
    }

    /// <inheritdoc />
    public async ValueTask<AgentSession?> CheckLivenessAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var process = _processLocator();

        if (process is null)
            return null;

        var headers = await _composerReader.ReadActiveHeadersAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        var state = EvaluateState(headers, process.Value.StartTimeUtc, now, out var lastActivity);

        return new AgentSession
        {
            Pid = process.Value.Pid,
            StartTimeUtc = process.Value.StartTimeUtc,
            State = state,
            LastActivityUtc = lastActivity ?? now
        };
    }

    private AgentSessionState EvaluateState(
        IReadOnlyList<CursorComposerHeaderDto> headers,
        DateTimeOffset processStartUtc,
        DateTimeOffset nowUtc,
        out DateTimeOffset? latestActivityUtc)
    {

        latestActivityUtc = null;
        var hasWaiting = false;

        foreach (var header in headers)
        {

            var checkpoint = header.LatestCheckpointUtc;

            if (checkpoint.HasValue && (latestActivityUtc is null || checkpoint > latestActivityUtc))
            {
                latestActivityUtc = checkpoint;
            }

            if (header.HasUnfinishedRun)
            {

                var checkpointTime = checkpoint ?? (header.UnfinishedRunAt.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(header.UnfinishedRunAt.Value)
                    : null);

                if (checkpointTime.HasValue && checkpointTime.Value >= processStartUtc && (nowUtc - checkpointTime.Value) <= _staleThreshold)
                {
                    return AgentSessionState.Busy;
                }
            }

            if (header.IsWaitingApproval)
            {
                hasWaiting = true;
            }
        }

        return hasWaiting ? AgentSessionState.Waiting : AgentSessionState.Idle;
    }

    private static (int Pid, DateTimeOffset StartTimeUtc)? DefaultProcessLocator()
    {
        try
        {
            var processes = Process.GetProcessesByName("Cursor");

            foreach (var p in processes)
            {
                try
                {
                    if (!p.HasExited)
                    {
                        var start = new DateTimeOffset(p.StartTime.ToUniversalTime(), TimeSpan.Zero);
                        return (p.Id, start);
                    }
                }
                catch
                {
                    // Ignore access denied or race conditions
                }
                finally
                {
                    p.Dispose();
                }
            }
        }
        catch
        {
            // Ignore process enumeration failures
        }

        return null;
    }
}
